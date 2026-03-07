# Nullable Phase 2 — Engine Internals (Complete)

- **Files updated:** Executive.cs, ExecutiveFastLight.cs, ExecFactory.cs, ModelConfig.cs, Model.cs, ExecEventRemover.cs.
- **Nullability updates:** `object userData` parameters annotated as `object?`, nullable events/fields adjusted, and Executive.SetCurrentEventController now accepts `DetachableEvent?`.
- **Heap internals:** ExecutiveFastLight queue/heap logic preserved; only annotations and null-forgiving used.
- **Build/Test:** `dotnet build Sage4-Everything.sln --no-incremental` succeeded; `dotnet test SageTestLib` total 319, passed 319.
- **Counts:** 23 files now `#nullable enable`; 519 files still `#nullable disable`.
