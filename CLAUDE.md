# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`Lnk` is a C# library (published to NuGet) that parses Windows shortcut (`.lnk`) files per MS-SHLLINK. It must tolerate malformed/hostile input and surface everything present in the file rather than silently normalizing it.

## Commands

The SDK is pinned in `global.json` (.NET 10). The library targets `netstandard2.0`; tests target `net10.0`. Both use `LangVersion 10` — avoid newer C# syntax.

```bash
dotnet build                                   # build solution (Lnk.slnx)
dotnet test                                    # run all tests (CI runs: dotnet test --configuration Release)
dotnet test --filter "FullyQualifiedName~SpecComplianceTests"          # one fixture
dotnet test --filter "Name=ShimDataBlockIsParsedAsShimDataBlock"       # one test
dotnet pack --configuration Release            # build NuGet package
```

Tests use NUnit 5 + FluentAssertions 7 (pinned to `[7.2.2,8.0.0)`, since 8.x needs a commercial license; Dependabot ignores it).

Versioning: bare CalVer tags like `2026.10.6` drive the package version (MinVer); there is no `<Version>` in `Lnk/Lnk.csproj`. To release: push to `master`, then `git tag <ver> && git push origin <ver> && gh release create <ver> --verify-tag --generate-notes`. CI (`.github/workflows/publish.yml`, identical to GuidMapping's) packs, validates, tests, and pushes to NuGet on GitHub release using trusted publishing (`NuGet/login` with the `NUGET_USER` repo variable).

## Architecture

Entry points: `Lnk.LoadFile(path, codepage)` (validates size/header signature, reads bytes) and the `LnkFile(byte[] ...)` constructors (used directly for carved data / jump list entries; `SourceFile` and `Source*` timestamps are null then).

`LnkFile` constructor parses the MS-SHLLINK sections sequentially, each gated by a `Header.DataFlag`:
1. `Header` (76 bytes)
2. LinkTargetIDList → `ShellItems/IdList.Parse` → `List<ExtensionBlocks.IShellBag>`
3. LinkInfo → `VolumeInfo` / `NetworkShareInfo`
4. StringData (Name, RelativePath, WorkingDirectory, Arguments, IconLocation)
5. ExtraData blocks → `LnkFile.ParseExtraBlock` switches on signature → `ExtraData/*` classes (all derive from `ExtraDataBase`)

### Shell items
Since 2026.10.6 the shell item classes are ExtensionBlocks' (shared with SBECmd, ShellBags Explorer and the RECmd plugins; Lnk's own copies were removed, after their fixes were brought there). `IdList.Parse` splits an ID list (MS-SHLLINK 2.2.1) and creates each item with `ExtensionBlocks.ShellItemFactory.Create(bytes, -1, -1, "", codepage)`; an item it doesn't know (`ShellBagUnknown`) or that throws becomes `ShellBagDamaged` (still in `Lnk.ShellItems`, now derived from `ExtensionBlocks.ShellBag`) with a readable reason in `ParseErrors`. `LnkFile.TargetIDs` and `VistaAndAboveIDListDataBlock.TargetIDs` are `List<ExtensionBlocks.IShellBag>`: a breaking change for code that used Lnk's classes (LECmd, JLECmd and JumpListExplorer switch on the shared types; ezTriage too). Before the move, the shared classes were checked to give the same `Value` and `FriendlyName` as Lnk's for every item of the corpus' LNK files (9,410) and, but for MTP and CD burning items that Lnk read as garbage, of the jump lists' LNKs (90,098); LECmd's and JLECmd's CSVs over them were the same. Item types now differ from Lnk's old ones where the shared factory has its own class (0x35, 0x36, 0x39, 0x3A, 0xB1, 0x41-0x47, CFSF items, MTP items), so consumers switch on those types, not on the old ones. Extension blocks (`BEEF0004` etc.) and GUID names come from ExtensionBlocks and GuidMapping.
### Error-handling model (important)
- Fatal structural problems throw `LnkParseException` (carries `Offset` and `Field`). The `LnkFile` constructor wraps any exception into one.
- Non-fatal problems do **not** throw: an unparseable shell item becomes `ShellBagDamaged`, an unparseable/unknown extra block becomes `DamagedDataBlock`, and a human-readable message is appended to `LnkFile.ParseErrors`. The rest of the file keeps parsing.
- Trailing data after the ExtraData terminal block is recorded (`TrailingDataSize`, `ExtraDataTerminated`) and reported in `ParseErrors` — attackers use it to smuggle payloads.
- StringData is never truncated (long padded `Arguments` is a known evasion technique); counts are unsigned 16-bit character counts.
- Use the bounds-checked readers in `Helpers` (`ReadUInt16/32`, `ReadGuid`, `Slice`, `EnsureRange`, `ReadAnsiZ/ReadUnicodeZ`, optional `limit` to keep reads inside a sub-structure) instead of raw `BitConverter` on untrusted offsets, so failures produce `LnkParseException` with a field name and oversized length fields can't trigger huge allocations.
- Text mode copies (a CR put before every LF byte, Issues #43): when the file throws or has `ParseErrors`, has CR LF pairs and no lone LF, and parses without errors once those CRs are removed, a note saying the file was converted to text mode is added to the exception message or `ParseErrors`. The data is still parsed as it is, never repaired.
- Header timestamps that are out of range become `DateTimeOffset.MinValue` and are reported, without affecting the other timestamps.

## Tests

- `Lnk.Test/TestMain.cs`: corpus tests over `Lnk.Test/TestFiles/<OS version>/*.test` (real LNK files, renamed `.test`), plus targeted assertions on specific samples. `TestFiles` is located by walking up from the test output dir, so tests work on Linux and Windows.
- `Lnk.Test/SpecComplianceTests.cs`: builds LNK bytes in memory with the `LnkBytes` helper to exercise individual spec rules and malformed-input handling. Prefer adding new spec/robustness tests here.
- `ITA_LNKs/` at the repo root is an optional extra corpus; tests using it are skipped when it is absent.
- `JumpListCorpusLnksParseWithoutErrors` runs every `.lnk`/`_lnk` in the local `Jump lists and lnks` collection (in `Corpus/` next to the repo, or directly next to it, or `JUMPLIST_CORPUS`; about 1,500 files, some private, never copy them into the repo) and expects no `ParseErrors`; skipped when absent. Those files were cross-checked field by field against LnkParse3.
