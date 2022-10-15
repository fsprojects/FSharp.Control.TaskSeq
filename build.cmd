@ECHO OFF

REM Make environment variables local to the batch script
SETLOCAL

REM Default local parameters (BUILD_MODE must remain empty, otherwise, 'help' doesn't work)
SET BUILD_CONFIG=Release
SET	BUILD_MODE=

SET DOTNET_TEST_ARGS=
SET DOTNET_TEST_PROJECT_LOCATION=

SET DOTNET_CI_ARGS=--blame-hang-timeout 60000ms --logger "trx;LogFileName=test-results-release.trx" --logger "console;verbosity=detailed"
SET DOTNET_TEST_ARGS=--logger "console;verbosity=detailed"
SET DOTNET_TEST_PROJECT_LOCATION=".\src\FSharpy.TaskSeq.Test\FSharpy.TaskSeq.Test.fsproj"

REM This is used to get a 'rest of arguments' list, which allows passing 
REM other arguments to the dotnet build and test commands
SET REST_ARGS=%*

:parseArgs
IF "%~1"=="build" (
	SET BUILD_MODE=build
	REM Drop 'build' from the remaining args
	CALL :shiftArg %REST_ARGS%

) ELSE IF "%~1"=="test" (
	SET BUILD_MODE=test
	REM Drop 'test' from the remaining args
	CALL :shiftArg %REST_ARGS%

) ELSE IF "%~1"=="ci" (
	SET BUILD_MODE=ci
	REM Drop 'ci' from the remaining args
	CALL :shiftArg %REST_ARGS%

) ELSE IF "%~1"=="" (
	REM No args, default: build
	SET BUILD_MODE=build
	SET BUILD_CONFIG=Release
)

CALL :tryBuildConfig %REST_ARGS%
ECHO Additional arguments: %REST_ARGS%

REM Main branching starts here
IF "%BUILD_MODE%"=="build" GOTO :runBuild
IF "%BUILD_MODE%"=="test" GOTO :runTest
IF "%BUILD_MODE%"=="ci" GOTO :runCi


REM Something wrong, we don't recognize the given arguments
REM Display help:

ECHO Argument not recognized
ECHO.
ECHO Available options are:
ECHO.
ECHO build     Run 'dotnet build' (default if omitted)
ECHO test      Run 'dotnet test' with default configuration and no CI logging.
ECHO ci        Run 'dotnet test' with CI configuration and TRX logging.
ECHO.
ECHO Optionally combined with:
ECHO.
ECHO release   Build release configuration (default).
ECHO debug     Build debug configuration.
ECHO.
ECHO Any arguments that follow the special arguments will be passed on to 'dotnet test' or 'dotnet build'
ECHO Such user-supplied arguments can only be given when one of the above specific commands is used.
ECHO
ECHO Optional arguments may be given with a leading '/' or '-', if so preferred.
ECHO.
ECHO Examples:
ECHO.
ECHO Run default build (release):
ECHO build
ECHO.
ECHO Run debug build:
ECHO build debug
ECHO.
ECHO Run debug build with detailed verbosity:
ECHO build debug --verbosity detailed
ECHO.
ECHO Run the tests in default CI configuration
ECHO build ci
ECHO.
ECHO Run the tests as in CI, but with the Debug configuration
ECHO build ci -debug
ECHO.
ECHO Run the tests without TRX logging
ECHO build test -release
ECHO.
GOTO :EOF

REM Normal building
:runBuild
ECHO Building for %BUILD_CONFIG% configuration...
ECHO.
ECHO Executing:
ECHO dotnet build src/FSharpy.TaskSeq.sln -c %BUILD_CONFIG% %REST_ARGS%
ECHO.
dotnet tool restore
dotnet build src/FSharpy.TaskSeq.sln -c %BUILD_CONFIG% %REST_ARGS%
GOTO :EOF

REM Testing
:runTest
ECHO.
ECHO Testing: %BUILD_CONFIG% configuration...
ECHO.
ECHO Restoring dotnet tools...
dotnet tool restore

ECHO Executing:
ECHO dotnet test -c %BUILD_CONFIG% %DOTNET_TEST_ARGS% %DOTNET_TEST_PROJECT_LOCATION% %REST_ARGS%
dotnet test -c %BUILD_CONFIG% %DOTNET_TEST_ARGS% %DOTNET_TEST_PROJECT_LOCATION% %REST_ARGS%
GOTO :EOF

REM Continuous integration
:runCi
ECHO.
ECHO Continuous integration: %BUILD_CONFIG% configuration...
ECHO.
ECHO Restoring dotnet tools...
dotnet tool restore

ECHO Executing:
ECHO dotnet test -c %BUILD_CONFIG% %DOTNET_CI_ARGS% %DOTNET_TEST_PROJECT_LOCATION% %REST_ARGS%
dotnet test -c %BUILD_CONFIG% %DOTNET_CI_ARGS% %DOTNET_TEST_PROJECT_LOCATION% %REST_ARGS%
GOTO :EOF


REM Callable label, will resume after 'CALL' line
:tryBuildConfig
IF "%~1"=="release" (
	SET BUILD_CONFIG=Release
	CALL :shiftArg %REST_ARGS%
)
IF "%~1"=="-release" (
	SET BUILD_CONFIG=Release
	CALL :shiftArg %REST_ARGS%
)
IF "%~1"=="/release" (
	SET BUILD_CONFIG=Release
	CALL :shiftArg %REST_ARGS%
)
IF "%~1"=="debug" (
	SET BUILD_CONFIG=Debug
	CALL :shiftArg %REST_ARGS%
)
IF "%~1"=="-debug" (
	SET BUILD_CONFIG=Debug
	CALL :shiftArg %REST_ARGS%
)
IF "%~1"=="/debug" (
	SET BUILD_CONFIG=Debug
	CALL :shiftArg %REST_ARGS%
)
GOTO :EOF

REM Callable label, will resume after 'CALL' line
:shiftArg
REM Do not call 'SHIFT' here, as we do it manually
REM Here, '%*' means the arguments given in the CALL command to this label
SET REST_ARGS=%*

REM Shift by stripping until and including the first argument
IF NOT "%REST_ARGS%"=="" CALL SET REST_ARGS=%%REST_ARGS:*%1=%%
GOTO :EOF
