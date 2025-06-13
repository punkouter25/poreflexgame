
GODOT .NET Development Rules for LLM
This document outlines the rules and workflow for developing a Godot 4.x application using C#. Adhere strictly to these guidelines.
Environment and Project Base:
You will be provided with a blank Godot 4.x project as a starting template.
All development must use Godot 4.x syntax and features.
The primary development language is C# (.NET).
Development Workflow - Step-by-Step:
The overall project development is broken down into approximately 10 predefined high-level steps.
You will focus on completing one high-level step at a time, as instructed by me. Do not attempt to complete multiple steps or move to the next step without my explicit instruction.
Each high-level step involves a cycle:
Modification: You will make necessary changes by directly modifying project files (see Rule 3). All .cs files should go in the Script folder.
Instruction: You will provide clear, step-by-step instructions on exactly what I need to do to manually test the functionality you've just implemented for that step within the Godot editor/running project.
Testing & Reporting: I will execute your testing instructions in Godot. After running the test, I will provide you with the full content of a log.txt file (see Rule 6).
Analysis & Next Action: You will analyze the provided log.txt content to verify the test outcome, identify any errors or unexpected behavior, and determine if the step was successful. Based on the analysis, you will either suggest modifications to fix issues within the current step or, if successful, await my instruction to proceed to the next high-level step.


File Modification - Direct Access:
You have direct file system access to the Godot project directory.
You are expected to make changes by directly modifying the content of Godot resource files (.tscn, .tres, .material, .gdshader, project.godot, etc.) and C# script files (.cs) located in the Script folder.
When modifying an existing file, you will output the entire modified content of that file, ensuring it is syntactically correct for the specific Godot resource format or C#.
Your changes are verified when Godot successfully loads and uses the modified files without parsing errors. You do not need to perform internal syntax checks beyond ensuring the output format is correct for the file type.
Scene vs. Code - Structure and Logic:
Scene Files (.tscn):
Define the static scene structure, node hierarchy, transforms (position, rotation, scale), and properties for individual, non-numerous objects directly within the .tscn file using Godot's text-based scene format.
Attach scripts (located in the Script folder) and resources to nodes in the .tscn file.


C# Code (.cs in Script folder):
Implement game logic, input handling, state management, UI updates, and signal connections (see Rule 5).
Programmatically create objects only when they are numerous, dynamic, non-static, or procedurally generated (e.g., spawning enemies, creating items from data). Avoid creating static scene elements in code if they can be defined in the .tscn.


Connecting Signals:
Prefer connecting signals programmatically in C# code using the Connect() method or C# events/actions where applicable.
If a standard signal connection is straightforward and commonly done directly in the .tscn file (e.g., connecting a Button's pressed signal to a parent node's script method), you may propose this, but programmatic connection in C# is the generally preferred approach for consistency and LLM ease.
Testing and Logging:
After delivering file modifications and/or code for a step, you must provide specific instructions on how I should test the functionality in Godot (e.g., "Run the 'MainScene.tscn' and press the left arrow key to verify the cube moves left.").
After I perform the test, I will provide you with the content of a log.txt file. You must analyze this log file for verification messages, errors, warnings, or any output related to the test run. This log.txt is your primary source of feedback from the Godot execution environment.
Assets and Resources:
Placeholder Assets: When a node requires an asset like a 3D model (.glb), 2D sprite (.png), or audio file (.wav), use simple Godot primitives (e.g., Mesh(Cube)) or default built-in resources as placeholders in the .tscn or .tres files. Assume I will replace these placeholders with the actual assets later. Do not attempt to generate or include external asset data.
Resource Files (.tres, etc.): When a node requires a custom resource (like a custom Material, MeshLib, Gradient, etc.), you should create a basic version of this resource by generating its full content and outputting it into a new .tres file (or the appropriate resource file extension). If the resource structure or creation is significantly complex or dynamic, you may instead provide C# code to create and configure it programmatically at runtime.
Manual Editor Actions:
You should strive to complete all tasks by directly modifying project files (.tscn, .cs, .tres, etc.).
Only ask me to perform actions manually in the Godot Editor GUI if the required setting, configuration, or action is not accessible or editable through direct text file modification (e.g., configuring project settings not exposed in project.godot, tasks requiring painting or complex visual interaction in the editor, importing specific complex assets that need import settings configuration). Justify why a manual step is necessary.
Primitives First:
During development and testing for each step, use simple, default Godot primitives (like Mesh(Cube), Quad, default materials) for visual representation. This aligns with using placeholder assets (Rule 7).
Error Handling and Debugging:
If the log.txt indicates errors or unexpected behavior during testing (Rule 6), analyze the log content to identify the likely cause.
Propose specific modifications to the relevant files (.cs in Script, .tscn, .tres) to fix the issue.
.NET Core Web API for High Scores:
A .NET Core Web API project will be created to store high scores. The Godot client application will communicate with this API. The server application will communicate with Azure Table Storage.
Azure Table Storage:
The .NET Core Web API will use Azure Table Storage as the primary database for storing high scores.
During local development, Azurite will be used for table storage emulation. An AzuriteData/ folder will be created in the root and added to .gitignore.
For deployment, Azure Table Storage will be configured using SAS connection strings stored in appsettings.Development.json locally and Azure App Service configuration settings in the deployed environment.
Azure Deployment:
When ready for deployment, a resource group named after the application (derived from the solution name from prd.md, starting with "Po") will be created if it doesn't exist.
A .bicep file will be created in the root directory to define and deploy the necessary Azure resources: an App Service and an Azure Table Storage account.
Individual Azure CLI commands to deploy these resources using the .bicep file will be provided.
A Log Analytics resource will be placed in the same resource group as the application.
Sensitive configuration will be managed using appsettings.Development.json for local development and Azure App Service configuration settings (environment variables) for the deployed application.
Application Insights:
A shared Application Insights resource located in the PoShared resource group will be used for monitoring and diagnostics of both the Godot client (if feasible) and the .NET Core Web API.
Solution Structure and Project Organization:
The solution name will be derived from prd.md and will always start with "Po".
A solution file (.sln) will be at the root level.
Each .csproj will reside in its own folder at the root level. For example, if the solution is PoMyGame, the API project might be in a folder named PoMyGame.Api with a PoMyGame.Api.csproj inside. The Godot project structure will remain within the root, but C# scripts will be in the Script folder.
The .bicep file for Azure deployment will be in the root directory.
GitHub Workflow for CI/CD:
GitHub workflow files for CI/CD will be set up in the .github/workflows directory. This workflow will build and deploy the necessary components (primarily the .NET Core Web API and potentially the Godot application if feasible) to the Azure resource group.
.vscode Configuration:
A .vscode directory containing launch.json, tasks.json, files will be configured for simple local deployment: pressing F5 in VS Code should run the .NET Core Web API locally. Running the Godot project will be done through the Godot editor.
.NET Framework:
Utilize the .NET 9.x framework (or latest stable version if 9.x is not yet officially stable) for the .NET Core Web API project. The Godot project will use the .NET integration supported by Godot 4.x.
Mandatory Diagnostics (Future Consideration):
While a Diag.razor page is specific to Blazor, we will consider implementing a similar diagnostic mechanism within the Godot application and the .NET Core Web API to verify connections (API health checks, Azure Table Storage status, etc.) and log these results. The specifics of this will be determined in later steps. Creat a Diag scene for godot projects
Development Approach:
Prioritize simplicity in design while ensuring solutions are architected for future expandability.
Focus on getting functionality working correctly before engaging in premature optimization.
Whenever encountering unused files or code, list all potentially removable items at once and ask the user for confirmation before removal.
Step Workflow & User Interaction:
For each of the 10 high-level steps in steps.md:
Plan & Design.
Test Stubs (XUnit for the .NET Core Web API).
Implement Logic & UI (Godot scenes/scripts and API controllers/services).
Log thoroughly (to Console, Serilog in the API, and log.txt).
Track Progress in steps.md.
Request Confirmation:
I've completed Step X: [Step Description].
The code compiles and all relevant tests pass (for the API).
Would you like me to:
Explain any part of the implementation in more detail
Make adjustments to the current step
Proceed to Step Y: [Next Step Description]


Wait for user confirmation.


Logging & Diagnostics Strategy (for .NET Core Web API):
Comprehensive Logging: Implement logging in the .NET Core Web API to: Console, Serilog, and the shared Application Insights.
log.txt File: A single log.txt file must be created (or overwritten) in the root directory (PoYourSolutionName/) at the start of each application run (both Godot and API, if feasible to consolidate or have separate logs clearly identifiable). It should contain the most recent information to help debug.
Log Content Requirements: Timestamps, component names, operation context. Detailed logging for database connections, API calls, errors. Log key decision points and state changes. Avoid repetitive logging.
Application Insights Integration: Track API requests, feature usage, performance metrics, exceptions. Create custom events for business operations. Set up availability tests.
Error Handling & Reliability (for .NET Core Web API):
Implement global exception handler middleware.
Use try/catch at service boundaries.
Return appropriate HTTP status codes from the API.
Log exceptions with context.
Consider Circuit Breaker pattern for external services (like Azure Table Storage).
Dependency Injection (DI) (for .NET Core Web API):
Follow standard DI practices (Transient, Scoped, Singleton).
Register services in the API's Program.cs or a dedicated service registration class.
Testing Approach (for .NET Core Web API):
Write XUnit tests for all new API functionality.
Use descriptive debug statements in the API code.
Focus XUnit tests on business logic and core API functionality (controllers, services, data access).
Verify API connections with test data. Dedicated connection tests for Azure Table Storage requiring connection details.
Data Storage & Management (for .NET Core Web API):
Data Storage Timeline & Location: Development: Azurite (AzuriteData/ in root, ignored by Git). Production: Azure Table Storage.
Azure Table Storage Implementation: Use repository patterns to interact with Azure Table Storage. Optimize partition and row keys based on data access patterns. Implement error handling for storage operations.
SAS Connection Strings: Store locally in appsettings.Development.json and in Azure App Service environment variables for the deployed API.
Deployment Process:
Local functionality of both Godot and the API will be the initial focus.
Azure deployment of the .NET Core Web API and Azure Table Storage will be done using the AI-provided Azure CLI commands based on the root-level .bicep file.
Configure environment-specific settings for the API during deployment.
Verify cloud connections after deployment.
The GitHub Actions CI/CD workflow will build and deploy the .NET Core Web API to the specified Azure resource group.
Feature Toggles/Flags (for .NET Core Web API):
Consider configuration-based feature toggles (using appsettings.json or Azure App Configuration/environment variables) for the API.
NuGet Package Management (for .NET Core Web API):
Use dotnet add package to add NuGet package dependencies to the API project. Document the purpose of each added package. Always use stable NuGet packages.
Localization:
English only for UI (within Godot) and messages/logs (from both Godot and the API). No multi-language support unless specified in prd.md.
Azure Best Practices (for .NET Core Web API and Azure resources):
Prefer Managed Services (Azure App Service, Azure Table Storage).
Implement Retry Policies for interactions with Azure services.
Use Connection Pooling for database and service connections.
Follow the principle of Least Privilege when configuring access.
Implement Azure Resource Tagging for the created resources.


