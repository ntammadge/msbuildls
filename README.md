# MSBuild Language Server

A language server to provide a complete development experience for MSBuild files (.targets, .props, .csproj, etc.).

A proper extension release for VSCode is not yet available. If you wish to use this, you will need to check out the source code and build the output files. To build, at the root directory run `npm install`, `npm run compile`, and `dotnet build`. Afterwards start debugging in your VSCode instance via the `Launch Client` configuration provided to use the language server in the debug instance.