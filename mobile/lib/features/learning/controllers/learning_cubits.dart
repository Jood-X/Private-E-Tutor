import '../../../core/api/api_failure.dart';
import '../../../core/api/api_result.dart';
import '../../../core/state/loadable.dart';
import '../../../core/state/loadable_cubit.dart';
import '../../content/models/content_models.dart';
import '../models/learning_models.dart';

/// A teacher's public profile and courses; lets the student ask to join a course.
class TeacherProfileCubit extends LoadableCubit<TeacherDetail> {
  TeacherProfileCubit({required LearningRepository repository, required String teacherId})
    : _repository = repository,
      super(() => repository.getTeacher(teacherId));

  final LearningRepository _repository;

  /// On success the course shows as pending without a reload.
  Future<ApiFailure?> requestToJoin(String courseId, {String? message}) async {
    final result = await _repository.requestEnrollment(courseId, message: message);
    if (isClosed) return null;
    switch (result) {
      case ApiSuccess(:final value):
        final teacher = state.value;
        if (teacher != null) {
          final course = teacher.courses.firstWhere((course) => course.id == courseId);
          emit(Loadable.loaded(teacher.withCourse(course.withStatus(value.status))));
        }
        return null;
      case ApiError(:final failure):
        return failure;
    }
  }
}

/// The student's enrollments ("My learning"). Withdrawing happens on the course screen.
class MyLearningCubit extends LoadableCubit<List<MyEnrollment>> {
  MyLearningCubit(LearningRepository repository) : super(repository.myEnrollments);
}

/// One course: summary for everyone, outline once approved. The student can withdraw from here.
class StudentCourseCubit extends LoadableCubit<StudentCourse> {
  StudentCourseCubit({required LearningRepository repository, required String courseId})
    : _repository = repository,
      super(() => repository.getCourse(courseId));

  final LearningRepository _repository;

  Future<ApiFailure?> withdraw() async {
    final enrollmentId = state.value?.enrollmentId;
    if (enrollmentId == null) return null;
    final result = await _repository.withdraw(enrollmentId);
    if (isClosed) return null;
    switch (result) {
      case ApiSuccess():
        await load();
        return null;
      case ApiError(:final failure):
        return failure;
    }
  }
}

/// A lesson the student can open (its course must be approved for them).
class StudentLessonCubit extends LoadableCubit<LessonDetail> {
  StudentLessonCubit({required LearningRepository repository, required String lessonId})
    : super(() => repository.getLesson(lessonId));
}
