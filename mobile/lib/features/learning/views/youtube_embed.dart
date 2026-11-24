import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:url_launcher/url_launcher.dart';
import 'package:webview_flutter/webview_flutter.dart';
import 'package:webview_flutter_android/webview_flutter_android.dart';

/// How the app identifies itself to YouTube's embedded player. YouTube requires embeds to send a Referer naming the
/// app; inside a WebView the documented way is an https URL derived from the application ID (com.etutor.etutor_app).
/// Without it YouTube refuses playback ("This video is unavailable", error 152/153).
const youtubeEmbedReferer = 'https://com.etutor.app/';

/// Hosts the embedded player may navigate within. Anything else (e.g. "Watch on YouTube") opens outside the app.
const _embedHosts = {'www.youtube-nocookie.com', 'www.youtube.com', 'youtube.com'};

/// The official YouTube embed in a WebView: YouTube's own controls (captions, quality, fullscreen), privacy-enhanced
/// mode (youtube-nocookie.com), and fullscreen shown in landscape on a separate route.
class YouTubeEmbed extends StatefulWidget {
  const YouTubeEmbed({required this.videoId, required this.languageCode, super.key});

  final String videoId;
  final String languageCode;

  /// The embed page for [videoId]. The student pressed "play", so it starts playing.
  static Uri embedUri(String videoId, String languageCode) => Uri.https('www.youtube-nocookie.com', '/embed/$videoId', {
    'autoplay': '1',
    'playsinline': '1',
    'rel': '0',
    'hl': languageCode,
    'cc_lang_pref': languageCode,
  });

  @override
  State<YouTubeEmbed> createState() => _YouTubeEmbedState();
}

class _YouTubeEmbedState extends State<YouTubeEmbed> {
  late final WebViewController _controller;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _controller =
        WebViewController()
          ..setJavaScriptMode(JavaScriptMode.unrestricted)
          ..setBackgroundColor(Colors.black)
          ..setNavigationDelegate(
            NavigationDelegate(
              onPageFinished: (_) {
                if (mounted) setState(() => _loading = false);
              },
              onNavigationRequest: (request) {
                final uri = Uri.tryParse(request.url);
                if (uri != null &&
                    uri.scheme == 'https' &&
                    _embedHosts.contains(uri.host) &&
                    uri.path.startsWith('/embed/')) {
                  return NavigationDecision.navigate;
                }
                // Links out of the player (title, logo, "Watch on YouTube") open in the YouTube app or browser.
                if (uri != null && uri.scheme == 'https') launchUrl(uri, mode: LaunchMode.externalApplication);
                return NavigationDecision.prevent;
              },
            ),
          )
          ..loadRequest(
            YouTubeEmbed.embedUri(widget.videoId, widget.languageCode),
            headers: const {'Referer': youtubeEmbedReferer},
          );

    if (_controller.platform case final AndroidWebViewController android) {
      android
        ..setMediaPlaybackRequiresUserGesture(false)
        ..setCustomWidgetCallbacks(onShowCustomWidget: _enterFullscreen, onHideCustomWidget: _exitFullscreen);
    }
  }

  bool _fullscreen = false;

  void _enterFullscreen(Widget video, void Function() onHidden) {
    _fullscreen = true;
    SystemChrome.setPreferredOrientations([DeviceOrientation.landscapeLeft, DeviceOrientation.landscapeRight]);
    SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
    Navigator.of(context, rootNavigator: true)
        .push(
          MaterialPageRoute<void>(
            fullscreenDialog: true,
            builder:
                (_) => PopScope(
                  // Back leaves fullscreen through the player, so it stays in sync.
                  canPop: false,
                  onPopInvokedWithResult: (didPop, _) {
                    if (!didPop) onHidden();
                  },
                  child: ColoredBox(color: Colors.black, child: Center(child: video)),
                ),
          ),
        )
        .whenComplete(_restoreOrientation);
  }

  void _exitFullscreen() {
    if (!_fullscreen) return;
    _fullscreen = false;
    Navigator.of(context, rootNavigator: true).pop();
  }

  void _restoreOrientation() {
    SystemChrome.setPreferredOrientations(DeviceOrientation.values);
    SystemChrome.setEnabledSystemUIMode(SystemUiMode.edgeToEdge);
  }

  @override
  void dispose() {
    if (_fullscreen) _restoreOrientation();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AspectRatio(
    aspectRatio: 16 / 9,
    child: ColoredBox(
      color: Colors.black,
      child: Stack(
        fit: StackFit.expand,
        children: [
          WebViewWidget(controller: _controller),
          if (_loading) const Center(child: CircularProgressIndicator(color: Colors.white)),
        ],
      ),
    ),
  );
}
