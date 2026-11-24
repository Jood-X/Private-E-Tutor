import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// One bottom-navigation tab.
class ShellTab {
  const ShellTab({required this.label, required this.icon, required this.selectedIcon});

  final String label;
  final IconData icon;
  final IconData selectedIcon;
}

/// The signed-in frame for a role: one tab per branch, each keeping its own navigation stack.
class RoleShell extends StatelessWidget {
  const RoleShell({required this.shell, required this.tabs, super.key});

  final StatefulNavigationShell shell;
  final List<ShellTab> tabs;

  @override
  Widget build(BuildContext context) => Scaffold(
    body: shell,
    bottomNavigationBar: NavigationBar(
      selectedIndex: shell.currentIndex,
      // Tapping the current tab again returns to its first screen.
      onDestinationSelected: (index) => shell.goBranch(index, initialLocation: index == shell.currentIndex),
      destinations: [
        for (final tab in tabs)
          NavigationDestination(icon: Icon(tab.icon), selectedIcon: Icon(tab.selectedIcon), label: tab.label),
      ],
    ),
  );
}
