import 'package:etutor_app/core/api/api_result.dart';
import 'package:etutor_app/core/api/paged.dart';
import 'package:etutor_app/features/content/models/content_models.dart';
import 'package:etutor_app/features/learning/models/learning_models.dart';
import 'package:etutor_app/features/teaching/models/teaching_models.dart';

const draftCourse = TeacherCourse(
  id: 'c-1',
  title: 'Physics 12',
  description: '',
  status: ContentStatus.draft,
  subjectName: 'Physics',
  gradeLevelName: 'Grade 12',
  units: [],
);

TeacherEnrollment pendingRequest(String id, {String student = 'Lina'}) => TeacherEnrollment(
  id: id,
  courseTitle: 'Physics 12',
  studentName: student,
  studentGradeName: 'Grade 12',
  status: EnrollmentStatus.pending,
  requestMessage: 'Please accept me',
);

/// In-memory [TeachingRepository]; tests set the answers they need and read what was sent.
class FakeTeachingRepository implements TeachingRepository {
  ApiResult<List<TeacherCourseSummary>> coursesResult = const ApiSuccess([]);
  ApiResult<TeacherCourse> courseResult = const ApiSuccess(draftCourse);

  /// Pages of enrollments by status; page N is `pages[N - 1]`.
  final Map<EnrollmentStatus, List<List<TeacherEnrollment>>> enrollmentPages = {};
  ApiResult<TeacherEnrollment>? decideResult;
  final List<(String, EnrollmentDecision, String?)> decisions = [];
  final List<int> requestedPages = [];

  @override
  Future<ApiResult<List<TeacherCourseSummary>>> listCourses() async => coursesResult;

  @override
  Future<ApiResult<TeacherCourse>> createCourse(NewCourse course) async => courseResult;

  @override
  Future<ApiResult<TeacherCourse>> getCourse(String courseId) async => courseResult;

  @override
  Future<ApiResult<TeacherCourse>> changeCourseStatus(String courseId, ContentStatus status) async => courseResult;

  @override
  Future<ApiResult<TeacherCourse>> addUnit(String courseId, String title) async => courseResult;

  @override
  Future<ApiResult<TeacherCourse>> changeUnitStatus(String unitId, ContentStatus status) async => courseResult;

  @override
  Future<ApiResult<TeacherCourse>> deleteUnit(String unitId) async => courseResult;

  @override
  Future<ApiResult<TeacherCourse>> addLesson(String unitId, String title) async => courseResult;

  @override
  Future<ApiResult<TeacherCourse>> changeLessonStatus(String lessonId, ContentStatus status) async => courseResult;

  @override
  Future<ApiResult<TeacherCourse>> deleteLesson(String lessonId) async => courseResult;

  @override
  Future<ApiResult<LessonDetail>> getLesson(String lessonId) async => const ApiSuccess(sampleLesson);

  @override
  Future<ApiResult<LessonDetail>> updateLesson(String lessonId, LessonEdit edit) async =>
      const ApiSuccess(sampleLesson);

  @override
  Future<ApiResult<LessonDetail>> addLink(String lessonId, {required String title, required String url}) async =>
      const ApiSuccess(sampleLesson);

  @override
  Future<ApiResult<LessonDetail>> deleteResource(String resourceId) async => const ApiSuccess(sampleLesson);

  @override
  Future<ApiResult<Paged<TeacherEnrollment>>> listEnrollments({required EnrollmentStatus status, int page = 1}) async {
    requestedPages.add(page);
    final pages = enrollmentPages[status] ?? const [<TeacherEnrollment>[]];
    return ApiSuccess(
      Paged(items: pages[page - 1], page: page, totalPages: pages.length, totalCount: pages.expand((p) => p).length),
    );
  }

  @override
  Future<ApiResult<TeacherEnrollment>> decide(String enrollmentId, EnrollmentDecision decision, {String? note}) async {
    decisions.add((enrollmentId, decision, note));
    return decideResult ?? ApiSuccess(pendingRequest(enrollmentId));
  }
}

const sampleLesson = LessonDetail(
  id: 'l-1',
  courseId: 'c-1',
  title: 'Newton\'s laws',
  summary: 'Forces and motion',
  notes: 'F = ma',
  status: ContentStatus.published,
  estimatedMinutes: 20,
  resources: [
    LessonResource(
      id: 'r-1',
      type: ResourceType.link,
      title: 'Simulation',
      isRequired: true,
      url: 'https://phet.colorado.edu/sim',
    ),
    LessonResource(
      id: 'r-2',
      type: ResourceType.document,
      title: 'Worksheet',
      isRequired: false,
      fileName: 'sheet.pdf',
    ),
  ],
);

TeacherCard teacherCard(String id, String name) =>
    TeacherCard(id: id, displayName: name, headline: 'Physics teacher', subjects: const ['Physics'], courseCount: 1);

const physicsCourse = PublicCourse(
  id: 'c-1',
  title: 'Physics 12',
  description: 'Mechanics and waves',
  subjectName: 'Physics',
  gradeLevelName: 'Grade 12',
  unitCount: 2,
  lessonCount: 6,
);

const khaled = TeacherDetail(
  id: 't-1',
  displayName: 'Khaled',
  headline: 'Physics teacher',
  bio: 'Ten years of Tawjihi physics.',
  isAcceptingStudents: true,
  courses: [physicsCourse],
);

/// In-memory [LearningRepository]; tests set the answers they need and read what was sent.
class FakeLearningRepository implements LearningRepository {
  /// Answers searches; receives the query and page.
  Future<ApiResult<Paged<TeacherCard>>> Function(TeacherQuery query, int page) onSearch =
      (_, page) async => ApiSuccess(Paged(items: const [], page: page, totalPages: 1, totalCount: 0));
  final List<(TeacherQuery, int)> searches = [];
  ApiResult<TeacherDetail> teacherResult = const ApiSuccess(khaled);
  ApiResult<MyEnrollment>? requestResult;
  final List<(String, String?)> requests = [];
  ApiResult<List<MyEnrollment>> enrollmentsResult = const ApiSuccess([]);
  ApiResult<StudentCourse>? courseResult;
  ApiResult<LessonDetail> lessonResult = const ApiSuccess(sampleLesson);

  @override
  Future<ApiResult<Paged<TeacherCard>>> searchTeachers(TeacherQuery query, {int page = 1}) {
    searches.add((query, page));
    return onSearch(query, page);
  }

  @override
  Future<ApiResult<TeacherDetail>> getTeacher(String teacherId) async => teacherResult;

  @override
  Future<ApiResult<MyEnrollment>> requestEnrollment(String courseId, {String? message}) async {
    requests.add((courseId, message));
    return requestResult ??
        ApiSuccess(
          MyEnrollment(
            id: 'e-1',
            courseId: courseId,
            courseTitle: 'Physics 12',
            subjectName: 'Physics',
            teacherName: 'Khaled',
            status: EnrollmentStatus.pending,
          ),
        );
  }

  @override
  Future<ApiResult<List<MyEnrollment>>> myEnrollments() async => enrollmentsResult;

  @override
  Future<ApiResult<MyEnrollment>> withdraw(String enrollmentId) async => ApiSuccess(
    MyEnrollment(
      id: enrollmentId,
      courseId: 'c-1',
      courseTitle: 'Physics 12',
      subjectName: 'Physics',
      teacherName: 'Khaled',
      status: EnrollmentStatus.withdrawn,
    ),
  );

  @override
  Future<ApiResult<StudentCourse>> getCourse(String courseId) async =>
      courseResult ??
      const ApiSuccess(
        StudentCourse(
          id: 'c-1',
          title: 'Physics 12',
          description: '',
          subjectName: 'Physics',
          gradeLevelName: 'Grade 12',
          teacherId: 't-1',
          teacherName: 'Khaled',
        ),
      );

  @override
  Future<ApiResult<LessonDetail>> getLesson(String lessonId) async => lessonResult;
}
