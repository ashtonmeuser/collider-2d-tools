# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-03-20

### Added

- Protected method `GetTagsHashCode` to hash tag collection.

### Changed

- `SvgCollider2D` now stores traversal tags in a deduplicated set instead of a list.
- Breaking: `SvgCollider2D` hook methods that receive `tags` now use `IReadOnlyCollection<string>` instead of `IReadOnlyList<string>`.
- `GetTagsHashCode` now produces the same value regardless of tag enumeration order, matching the new set-based tag model.

## [0.1.3] - 2026-03-15

### Added

- Enable/disable visualizing disabled colliders.
- Add `OnDocumentCreated` callback to pass SVG document info.

### Changed

- Child colliders inherit target Game Object layer.

## [0.1.2] - 2026-03-11

### Added

- Provide last group ID callbacks.
- Allow skipping branches via `ShouldDescend` callback.
- Allow disabling SVG collider parser.
- Allow disabling collider visualizer.
- Accept Regex tag pattern.

### Changed

- `Walk` methods are now public.
- Properly handle missing geometry allowed by SVG e.g. rect `x`, circle `cx`.

## [0.1.1] - 2026-03-03

### Removed

- Symlink to README removed as it's incompatible with Unity.

## [0.1.0] - 2026-03-03

### Added

- `SvgCollider2D` class to create colliders for shapes parsed from an SVG file.
- `ColliderVisualizer2D` class to create visible meshes for 2D collider shapes.
