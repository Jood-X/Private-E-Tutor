import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../app/routes.dart';
import '../../../core/state/loadable.dart';
import '../../../design_system/components/app_choice_pill.dart';
import '../../../design_system/components/app_flow_scaffold.dart';
import '../../../design_system/components/app_text_field.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../auth/controllers/form_submission.dart';
import '../../auth/views/widgets/form_errors.dart';
import '../../content/views/content_labels.dart';
import '../../onboarding/models/catalog_models.dart';
import '../controllers/create_course_cubit.dart';

class CreateCourseScreen extends StatelessWidget {
  const CreateCourseScreen({required this.createController, super.key});

  final CreateCourseCubit Function() createController;

  @override
  Widget build(BuildContext context) =>
      BlocProvider(create: (_) => createController()..load(), child: const _CreateCourseView());
}

class _CreateCourseView extends StatefulWidget {
  const _CreateCourseView();

  @override
  State<_CreateCourseView> createState() => _CreateCourseViewState();
}

class _CreateCourseViewState extends State<_CreateCourseView> {
  final _title = TextEditingController();
  final _description = TextEditingController();

  @override
  void dispose() {
    _title.dispose();
    _description.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final cubit = context.read<CreateCourseCubit>();
    final state = context.watch<CreateCourseCubit>().state;

    return BlocListener<CreateCourseCubit, CreateCourseState>(
      listenWhen: (previous, current) => previous.created == null && current.created != null,
      listener: (context, state) => context.pushReplacement(AppRoutes.courseEditor(state.created!.id)),
      child: AppFlowScaffold(
        title: l10n.createCourseTitle,
        subtitle: l10n.catalogPlacementHint,
        primaryAction: AppPrimaryButton(
          label: l10n.createCourseCta,
          busy: state.submitting,
          onPressed: state.canSubmit ? () => cubit.submit(title: _title.text, description: _description.text) : null,
        ),
        children: [
          if (state.failure != null) FormFailureBanner(form: FormSubmission.failed(state.failure!), fields: const {}),
          AppTextField(label: l10n.fieldCourseTitle, controller: _title, textInputAction: TextInputAction.next),
          const SizedBox(height: AppSpacing.lg),
          AppTextField(label: l10n.fieldDescription, controller: _description),
          const SizedBox(height: AppSpacing.lg),
          _Pills<Curriculum>(
            label: l10n.onboardingCurriculumLabel,
            loadable: state.curricula,
            onRetry: cubit.load,
            idOf: (item) => item.id,
            nameOf: (item) => item.name,
            selectedId: state.curriculumId,
            onSelected: (item) => cubit.selectCurriculum(item.id),
          ),
          if (state.curriculumId != null) ...[
            const SizedBox(height: AppSpacing.lg),
            _Pills<CatalogItem>(
              label: l10n.onboardingGradeLabel,
              loadable: state.grades,
              onRetry: () => cubit.selectCurriculum(state.curriculumId!),
              idOf: (item) => item.id,
              nameOf: (item) => item.name,
              selectedId: state.gradeLevelId,
              onSelected: (item) => cubit.selectGrade(item.id),
            ),
          ],
          if (state.gradeLevelId != null) ...[
            const SizedBox(height: AppSpacing.lg),
            _Pills<CatalogItem>(
              label: l10n.onboardingSubjectsLabel,
              loadable: state.subjects,
              onRetry: () => cubit.selectGrade(state.gradeLevelId!),
              idOf: (item) => item.id,
              nameOf: (item) => item.name,
              selectedId: state.subjectId,
              onSelected: (item) => cubit.selectSubject(item.id),
            ),
          ],
          const SizedBox(height: AppSpacing.lg),
          Text(l10n.fieldContentLanguage, style: Theme.of(context).textTheme.titleSmall),
          const SizedBox(height: AppSpacing.xs),
          Wrap(
            spacing: AppSpacing.xs,
            children: [
              AppChoicePill(
                label: l10n.languageEnglish,
                selected: state.contentLanguage == 'en',
                onPressed: () => cubit.selectLanguage('en'),
              ),
              AppChoicePill(
                label: l10n.languageArabic,
                selected: state.contentLanguage == 'ar',
                onPressed: () => cubit.selectLanguage('ar'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _Pills<T> extends StatelessWidget {
  const _Pills({
    required this.label,
    required this.loadable,
    required this.onRetry,
    required this.idOf,
    required this.nameOf,
    required this.selectedId,
    required this.onSelected,
  });

  final String label;
  final Loadable<List<T>> loadable;
  final VoidCallback onRetry;
  final int Function(T) idOf;
  final String Function(T) nameOf;
  final int? selectedId;
  final void Function(T) onSelected;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: Theme.of(context).textTheme.titleSmall),
        const SizedBox(height: AppSpacing.xs),
        LoadableView(
          loadable: loadable,
          onRetry: onRetry,
          builder:
              (items) => Wrap(
                spacing: AppSpacing.xs,
                runSpacing: AppSpacing.xs,
                children: [
                  for (final item in items)
                    AppChoicePill(
                      label: nameOf(item),
                      selected: idOf(item) == selectedId,
                      onPressed: () => onSelected(item),
                    ),
                ],
              ),
        ),
      ],
    );
  }
}
