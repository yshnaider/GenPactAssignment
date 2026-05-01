# GenPactAssignment

Playwright + NUnit (.NET) test project with Allure reporting.

## Project structure

- `PlaywrightTests/` — test project (NUnit + Playwright for .NET)
- `PlaywrightTests/pageModels/` — page objects (Wikipedia UI interactions)
- `PlaywrightTests/utils/` — helpers (Allure utilities, Wikipedia API parsing, word parsing)

## Prerequisites

- .NET SDK installed (`dotnet --info`)
- Allure CLI installed (to view reports)
  - If you use Scoop: `scoop install allure`

## Install Playwright browsers

From the test project folder:

```powershell
Set-Location .\PlaywrightTests
powershell -ExecutionPolicy Bypass -File .\bin\Debug\net10.0\playwright.ps1 install
```

If the script is not present yet, build once first:

```powershell
Set-Location .\PlaywrightTests
dotnet build
powershell -ExecutionPolicy Bypass -File .\bin\Debug\net10.0\playwright.ps1 install
```

## Run tests

Run all tests:

```powershell
Set-Location .\PlaywrightTests
dotnet test -v minimal
```

Run a single test:

```powershell
Set-Location .\PlaywrightTests
dotnet test -v minimal --filter FullyQualifiedName=PlaywrightTests.Task1Tests.CompareUniqueWords
```

## Allure results and report

- Allure results directory: `PlaywrightTests/allure-results/`
- To open the report:

```powershell
Set-Location .\PlaywrightTests
allure serve .\allure-results
```

## Notes

- Browser runs in **headed** mode by default (see `PlaywrightTests/Framework/PlaywrightTestBase.cs`).
- On failing tests, the teardown attempts to attach artifacts (HTML, URL, screenshot, video) to Allure.

