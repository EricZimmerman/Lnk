# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`Lnk` is a C# library (published to NuGet) that parses Windows shortcut (`.lnk`) files per MS-SHLLINK. It must tolerate malformed/hostile input and surface everything present in the file rather than silently normalizing it.

## Commands

The SDK is pinned in `global.json` (.NET 10). The library targets `netstandard2.0`; tests target `net10.0`. Both use `LangVersion 10` — avoid newer C# syntax.

```bash
dotnet build                                   # build solution (Lnk.sln)
dotnet test                                    # run all tests (CI runs: dotnet test --configuration Release)
dotnet test --filter "FullyQualifiedName~SpecComplianceTests"          # one fixture
dotnet test --filter "Name=ShimDataBlockIsParsedAsShimDataBlock"       # one test
dotnet pack --configuration Release            # build NuGet package
```

Tests use NUnit 4 + FluentAssertions 7 (pinned to `[7.0.0,8.0.0)`).

Versioning: tags like `2026.5.3` drive the package version (MinVer); `<Version>` in `Lnk/Lnk.csproj` is also bumped manually per release. CI (`.github/workflows/publish.yml`) packs, validates, tests, and pushes to NuGet on GitHub release.

## Architecture

Entry points: `Lnk.LoadFile(path, codepage)` (validates size/header signature, reads bytes) and the `LnkFile(byte[] ...)` constructors (used directly for carved data / jump list entries; `SourceFile` and `Source*` timestamps are null then).

`LnkFile` constructor parses the MS-SHLLINK sections sequentially, each gated by a `Header.DataFlag`:
1. `Header` (76 bytes)
2. LinkTargetIDList → `ShellItems/ShellItemFactory.ParseIdList` → list of `ShellBag` subclasses
3. LinkInfo → `VolumeInfo` / `NetworkShareInfo`
4. StringData (Name, RelativePath, WorkingDirectory, Arguments, IconLocation)
5. ExtraData blocks → `LnkFile.ParseExtraBlock` switches on signature → `ExtraData/*` classes (all derive from `ExtraDataBase`)

### Shell items
`ShellItemFactory.Create` dispatches on the item type byte (offset 2) to `ShellBag0x??` classes (several type bytes map to one class; zip-contents items are detected heuristically before the switch). Shell item extension blocks (`BEEF0004` etc.) are parsed by the external `ExtensionBlocks` NuGet package, not this repo; GUID → name mapping comes from the `GuidMapping` package. `VistaAndAboveIDListDataBlock` reuses the same factory.

### Error-handling model (important)
- Fatal structural problems throw `LnkParseException` (carries `Offset` and `Field`). The `LnkFile` constructor wraps any exception into one.
- Non-fatal problems do **not** throw: an unparseable shell item becomes `ShellBagDamaged`, an unparseable/unknown extra block becomes `DamagedDataBlock`, and a human-readable message is appended to `LnkFile.ParseErrors`. The rest of the file keeps parsing.
- Trailing data after the ExtraData terminal block is recorded (`TrailingDataSize`, `ExtraDataTerminated`) and reported in `ParseErrors` — attackers use it to smuggle payloads.
- StringData is never truncated (long padded `Arguments` is a known evasion technique); counts are unsigned 16-bit character counts.
- Use the bounds-checked readers in `Helpers` (`ReadUInt16/32`, `ReadGuid`, `Slice`, `EnsureRange`, `ReadAnsiZ/ReadUnicodeZ`, optional `limit` to keep reads inside a sub-structure) instead of raw `BitConverter` on untrusted offsets, so failures produce `LnkParseException` with a field name and oversized length fields can't trigger huge allocations.
- Header timestamps that are out of range become `DateTimeOffset.MinValue` and are reported, without affecting the other timestamps.

## Tests

- `Lnk.Test/TestMain.cs`: corpus tests over `Lnk.Test/TestFiles/<OS version>/*.test` (real LNK files, renamed `.test`), plus targeted assertions on specific samples. `TestFiles` is located by walking up from the test output dir, so tests work on Linux and Windows.
- `Lnk.Test/SpecComplianceTests.cs`: builds LNK bytes in memory with the `LnkBytes` helper to exercise individual spec rules and malformed-input handling. Prefer adding new spec/robustness tests here.
- `ITA_LNKs/` at the repo root is an optional extra corpus; tests using it are skipped when it is absent.
