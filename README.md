# MyWealth

The project was generated using the [Clean.Architecture.Solution.Template](https://github.com/jasontaylordev/CleanArchitecture) version 10.8.0.

## Documentation

- **Shared design docs** live in `docs/`  
  (function plan, architecture, domain model, database, API, feature specs, ADRs).  
  These are the source of truth. Prefer them when making decisions or generating code.

- **Personal working notes** go in `local_docs/`  
  (ideas, research-notes, scratch, session-log).  
  These are temporary, informal, and often outdated.  
  Do **not** treat them as authoritative. Only read them when the user explicitly asks or the current task clearly requires it.

## Build

Run `dotnet build` to build the solution.

## Run

To run the application:

```bash
dotnet run --project .\src\AppHost
```

The Aspire dashboard will open automatically, showing the application URLs and logs.

## Code Styles & Formatting

The template includes [EditorConfig](https://editorconfig.org/) support to help maintain consistent coding styles for multiple developers working on the same project across various editors and IDEs. The **.editorconfig** file defines the coding styles applicable to this solution.

## Code Scaffolding

The template includes support to scaffold new commands and queries.

Start in the `.\src\Application\` folder.

Create a new command:

```
dotnet new ca-usecase --name Login --feature-name IdentityAuth --usecase-type command --return-type LoginResultVm
```

Create a new query:

```
dotnet new ca-usecase -n GetCurrentUser -fn IdentityAuth -ut query -rt CurrentUserVm
```

If you encounter the error *"No templates or subcommands found matching: 'ca-usecase'."*, install the template and try again:

```bash
dotnet new install Clean.Architecture.Solution.Template::10.8.0
```

## Test

The solution contains unit, integration, and functional tests.

To run the tests:
```bash
dotnet test
```

## Help
To learn more about the template go to the [project website](https://cleanarchitecture.jasontaylor.dev). Here you can find additional guidance, request new features, report a bug, and discuss the template with other users.