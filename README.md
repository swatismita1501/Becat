# EcatDesktop

This solution recreates the Paradox menu, setup, queue, and archive screens as a self-contained C# Windows Forms app targeting .NET Framework 4.0.

## Current CAT workflow

The Job Queue screen now includes the Paradox-style workflow buttons:

1. **Run Jobs** - validates the selected CAT input pack and creates the CAT working/output files.
2. **Check Jobs** - marks successfully processed jobs as reviewed.
3. **Cleanup / Done Queue** - moves reviewed jobs from the active queue into the done/archive queue.
4. **Archive Server** - opens the archive screen so reviewed jobs can be zipped/archived.

For the HHH January CAT job, place the Paradox input files here:

```text
C:\RemoteEcat\ECAT_RAW\HHHJAN\
```

Required HHH input files:

```text
HHH_I.DAT
HHH_LDESC.DAT
HHH_DE.DAT
HHH_CL.DAT
HHH_FI.DAT
HHH_VM.DAT
```

The HHH CAT flow does not require `HHH_U.052`, `HHH_U.DAT`, or `ITEMEXT.DAT`.

## Configuration files

The seed config now defaults to C-drive paths:

```text
C:\RemoteEcat\ECAT_RAW\
C:\RemoteEcat\ECAT_ASC\
C:\RemoteEcat\ARCHIVE\
C:\ECAT_WRK\
```

On first launch the app copies `Data\Seeds\*.seed.csv` to live CSV files in the runtime `Data` folder beside the executable. After the app has run once, edit the live runtime files, not only the seed files.

Typical runtime files:

```text
bin\Debug\Data\general-config.csv
bin\Debug\Data\archive-config.csv
bin\Debug\Data\queue.csv
bin\Debug\Data\done.csv
```

## Setup fidelity update

The setup screen now follows the original Paradox behavior more closely when a CAT job is being created or edited:

- `JobID` continues to derive from `WhlCode + CatMonth`
- if a matching catalog item file already exists for that job, the app now uses that file's last-write date as the suggested `FromDate`
- the lookup checks both configured CAT archive and raw roots to match the way migrated environments often store the active source pack

## Notes

- Paradox tables were converted into CSV-backed application files under `App/Data`.
- No database driver, Paradox runtime, or third-party package is required by the project.
- Archive output uses built-in framework packaging support rather than the old PKZIP batch flow.
- The container used for this conversion does not include a .NET Framework 4.0 build toolchain, so the solution could not be compiled here. The project is structured as a classic Visual Studio solution and project for Windows.

## HHH CAT fidelity update

For House-Hasson CAT jobs (`WhlCode=HHH`), the project now uses the six-file source pack used by the Paradox workflow:

- `HHH_I.DAT`
- `HHH_LDESC.DAT`
- `HHH_DE.DAT`
- `HHH_CL.DAT`
- `HHH_FI.DAT`
itemext.dat is also used as input file.

The HHH CAT flow now has two modes:

- HHH CAT output is generated from the staged HHH input pack; no reference ZIP is embedded.
- other HHH CAT jobs now run through a real C# parsing/export path instead of the old raw-file pass-through.

That generated path now:

- parses staged item, long-description, vendor, department, class, and fineline inputs
- derives distinct dept/class/fine/vendor exports from the parsed item set
- builds fixed-width `ITEM.TXT`, `VEND.TXT`, `DEPT.TXT`, `CLAS.TXT`, and `FINE.TXT` files in the expected archive package shape

The long-description file is still copied into the local/archive DB staging files as `ITEMEXT.DB` so it remains in the job package path.

Note: full byte-for-byte proof against Paradox still requires broader validation against known-good non-APR House-Hasson output.

## Working-file staging update

For non-HHH CAT, EFM, and INH jobs, the prep layer now stages the actual validated source files into the local working directory before any downstream processing runs.

Additional staging improvements now in place:

- non-HHH CAT source files populate `ITEM_RAW.DB`, `UPC_RAW.DB`, and `ITEMEXT.DB` from the real input pack
- in-house source files populate the corresponding raw DB artifacts from the real matched inputs when those inputs exist
- generated DB artifacts are created from staged inputs and transformation output; no shipped DB templates are required

## Generic CAT generation update

Non-HHH CAT jobs no longer emit placeholder export text by default.

The current generic CAT path now:

- parses the staged item source file into derived item rows
- merges staged UPC data by SKU when available
- merges staged long-description data by SKU when available
- generates `ITEM.TXT`, `VEND.TXT`, `DEPT.TXT`, `CLAS.TXT`, and `FINE.TXT` from the parsed item set

This is a practical parsed export path for generic CAT jobs, but it is still heuristic until each wholesaler's source layout is validated directly against known-good Paradox output.

## EJD CAT port

`EJD` CAT jobs now bypass the generic parsed path and use an EJD-specific transformation flow built from the original Paradox `stdcat` routines:

- item generation mapped from the Paradox `itemIntoOut` flow
- vendor generation mapped from `StdVndOut`
- department generation mapped from `makeecde`
- class generation mapped from `makeeccl`
- fineline generation mapped from `makeecfi`

The EJD path now reads the staged fixed-width `ejd_i.dat`, `ejd_ldesc.dat`, `ejd_de.dat`, and `ejd_cl.dat` source files directly, and it uses `ejd_vm.dat` when that vendor file is available. If the `_vm` file is missing, the CAT build still runs and falls back to generated vendor rows without the joined vendor master details.

The current EJD implementation has also been tuned against the supplied known-good EJD output family so the item, vendor, department, and class stages align more directly with the observed Paradox export behavior for that source pack.

Vendor Input for HHH
vendor input for HHH is taken from Reference\LTVM.db and processed and generated output VEND.txt

## Windows export filename handling

Generated CAT exports are now normalized safely when the app writes mixed-case names such as `ITEM.TXT` and then needs the legacy lowercase names such as `item.txt`. On Windows, those paths refer to the same file, so the export helper now performs a safe temporary rename instead of trying to copy a file onto itself with case-only differences.


## Reference-output policy

This build does not ship or use baked-in Paradox output ZIPs. CAT output is generated from the staged input files and the ported transformation logic.

## Error Handling and Logging Update

This build adds centralized application logging and standardized exception handling across startup, UI event handlers, queue processing, archive processing, setup services, and file-contract validation.

Runtime log path:

```text
<application folder>\Logs\EcatDesktop-yyyyMMdd.log
```

Key changes:

- `Program.cs` now hooks `Application.ThreadException` and `AppDomain.CurrentDomain.UnhandledException`.
- `Common\Log.cs` is fail-safe and writes startup, shutdown, queue, setup, archive, and file-processing events.
- `Common\Error.cs` is used for standardized error dialogs and centralized exception reporting.
- Job Queue, Setup, Archive, and Main form event handlers now guard UI actions with try/catch.
- `JobProcessingService`, `JobSetupService`, `ArchiveService`, and `JobFileContractService` now log operational steps and return standardized `OperationResult` failures for handled errors.
- File/archive/data-store initialization errors are logged before rethrowing.


## Stored-output and template policy

This build does not ship stored Paradox output ZIPs or Paradox DB templates. Runtime artifacts are generated from staged input files and transformation logic.
