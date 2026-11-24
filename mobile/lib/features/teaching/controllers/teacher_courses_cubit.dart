import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../core/api/api_failure.dart';
import '../../../core/api/api_result.dart';
import '../../../core/state/loadable.dart';
import '../../content/models/content_models.dart';
import '../models/teaching_models.dart';

/// "My courses" list.
class TeacherCoursesCubit extends Cubit<Loadable<List<TeacherCourseSummary>>> {
  TeacherCoursesCubit(this._repository) : super(const Loadable.idle());

  final TeachingRepository _repository;

  Future<void> load() async {
    emit(Loadable.loading(state.value));
    final result = await _repository.listCourses();
    if (!isClosed) emit(Loadable.from(result));
  }
}

/// Editor for one course's outline. Every change returns the refreshed outline from the server.
/// Actions return the failure (or null) so the view can show it without losing the current outline.
class CourseEditorCubit extends Cubit<Loadable<TeacherCourse>> {
  CourseEditorCubit({required TeachingRepository repository, required String courseId})
    : _repository = repository,
      _courseId = courseId,
      super(const Loadable.idle());

  final TeachingRepository _repository;
  final String _courseId;

  Future<void> load() async {
    emit(Loadable.loading(state.value));
    final result = await _repository.getCourse(_courseId);
    if (!isClosed) emit(Loadable.from(result));
  }

  Future<ApiFailure?> setCourseStatus(ContentStatus status) =>
      _apply(_repository.changeCourseStatus(_courseId, status));

  Future<ApiFailure?> addUnit(String title) => _apply(_repository.addUnit(_courseId, title));

  Future<ApiFailure?> setUnitStatus(String unitId, ContentStatus status) =>
      _apply(_repository.changeUnitStatus(unitId, status));

  Future<ApiFailure?> deleteUnit(String unitId) => _apply(_repository.deleteUnit(unitId));

  Future<ApiFailure?> addLesson(String unitId, String title) => _apply(_repository.addLesson(unitId, title));

  Future<ApiFailure?> setLessonStatus(String lessonId, ContentStatus status) =>
      _apply(_repository.changeLessonStatus(lessonId, status));

  Future<ApiFailure?> deleteLesson(String lessonId) => _apply(_repository.deleteLesson(lessonId));

  Future<ApiFailure?> _apply(Future<ApiResult<TeacherCourse>> request) async {
    final result = await request;
    if (isClosed) return null;
    switch (result) {
      case ApiSuccess(:final value):
        emit(Loadable.loaded(value));
        return null;
      case ApiError(:final failure):
        return failure;
    }
  }
}

/// Editor for one lesson: text fields and resources.
class LessonEditorCubit extends Cubit<Loadable<LessonDetail>> {
  LessonEditorCubit({required TeachingRepository repository, required String lessonId})
    : _repository = repository,
      _lessonId = lessonId,
      super(const Loadable.idle());

  final TeachingRepository _repository;
  final String _lessonId;

  Future<void> load() async {
    emit(Loadable.loading(state.value));
    final result = await _repository.getLesson(_lessonId);
    if (!isClosed) emit(Loadable.from(result));
  }

  Future<ApiFailure?> save(LessonEdit edit) => _apply(_repository.updateLesson(_lessonId, edit));

  Future<ApiFailure?> addLink({required String title, required String url}) =>
      _apply(_repository.addLink(_lessonId, title: title, url: url));

  Future<ApiFailure?> deleteResource(String resourceId) => _apply(_repository.deleteResource(resourceId));

  Future<ApiFailure?> _apply(Future<ApiResult<LessonDetail>> request) async {
    final result = await request;
    if (isClosed) return null;
    switch (result) {
      case ApiSuccess(:final value):
        emit(Loadable.loaded(value));
        return null;
      case ApiError(:final failure):
        return failure;
    }
  }
}
