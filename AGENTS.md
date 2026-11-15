# Agent notes

Start with README.md. It covers how to run the CLI, seed files and configuration.

- To check a seed or a filter, run it. Replaying a seed list takes milliseconds:
  - `dotnet run --project Motely.CLI -- --jaml <filter> --seeds <file|list|-> --cutoff off`
- Fix the code as it stands now. Don't dig through git history to justify a change.
- Seed lists are plain files (motely CSV output: `seed,score,...`). There is no database.
- Tests: `dotnet test Motely.Tests` or `dotnet test Motely.slnx`. The solution builds cleanly across projects.
- Search-mode parsing for the CLI lives in `Motely/CliSearchMode.cs`.
