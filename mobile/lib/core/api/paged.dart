import 'package:equatable/equatable.dart';

/// One page of a paginated API list: `{ items, page, pageSize, totalCount, totalPages }`.
class Paged<T> extends Equatable {
  const Paged({required this.items, required this.page, required this.totalPages, required this.totalCount});

  factory Paged.decode(Object? data, T Function(Map<String, dynamic>) decodeItem) {
    final json = data! as Map<String, dynamic>;
    return Paged(
      items: (json['items'] as List<Object?>).cast<Map<String, dynamic>>().map(decodeItem).toList(growable: false),
      page: json['page'] as int,
      totalPages: json['totalPages'] as int,
      totalCount: json['totalCount'] as int,
    );
  }

  final List<T> items;
  final int page;
  final int totalPages;
  final int totalCount;

  bool get hasMore => page < totalPages;

  @override
  List<Object?> get props => [items, page, totalPages, totalCount];
}
