import 'package:flutter/material.dart';

import '../tokens/app_colors.dart';
import '../tokens/app_dimensions.dart';

/// Labelled text input with persistent helper text and an inline error under the field.
/// Password fields get a show/hide toggle with an accessible label.
class AppTextField extends StatefulWidget {
  const AppTextField({
    required this.label,
    required this.controller,
    this.helper,
    this.error,
    this.keyboardType,
    this.textInputAction,
    this.autofillHints,
    this.obscure = false,
    this.showPasswordLabel,
    this.hidePasswordLabel,
    this.onSubmitted,
    this.onChanged,
    this.enabled = true,
    super.key,
  });

  final String label;
  final TextEditingController controller;
  final String? helper;
  final String? error;
  final TextInputType? keyboardType;
  final TextInputAction? textInputAction;
  final Iterable<String>? autofillHints;
  final bool obscure;
  final String? showPasswordLabel;
  final String? hidePasswordLabel;
  final ValueChanged<String>? onSubmitted;
  final ValueChanged<String>? onChanged;
  final bool enabled;

  @override
  State<AppTextField> createState() => _AppTextFieldState();
}

class _AppTextFieldState extends State<AppTextField> {
  late bool _hidden = widget.obscure;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Visible label; screen readers get it from the Semantics wrapper on the input instead.
        ExcludeSemantics(child: Text(widget.label, style: textTheme.titleSmall)),
        const SizedBox(height: AppSpacing.xs - 2),
        Semantics(
          label: widget.label,
          child: TextField(
            controller: widget.controller,
            enabled: widget.enabled,
            keyboardType: widget.keyboardType,
            textInputAction: widget.textInputAction,
            autofillHints: widget.autofillHints,
            obscureText: _hidden,
            enableSuggestions: !widget.obscure,
            autocorrect: !widget.obscure,
            onSubmitted: widget.onSubmitted,
            onChanged: widget.onChanged,
            style: textTheme.bodyLarge,
            decoration: InputDecoration(
              errorText: widget.error,
              errorMaxLines: 3,
              helperText: widget.error == null ? widget.helper : null,
              helperMaxLines: 3,
              suffixIcon:
                  widget.obscure
                      ? IconButton(
                        tooltip: _hidden ? widget.showPasswordLabel : widget.hidePasswordLabel,
                        icon: Icon(
                          _hidden ? Icons.visibility_outlined : Icons.visibility_off_outlined,
                          color: AppColors.inkMuted,
                        ),
                        onPressed: () => setState(() => _hidden = !_hidden),
                      )
                      : null,
            ),
          ),
        ),
      ],
    );
  }
}
