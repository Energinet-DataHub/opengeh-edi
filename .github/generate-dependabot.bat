@echo off

:: Makes sure the variables that get assigned are local instead of global
setlocal

:: The output filename
set dependabot_file=dependabot.yml

:: Call the functions that make up the program
call :setDirVariables
call :generateFile
call :run

echo Done
endlocal

:: Stops the program
goto :eof

:generateFile
    :: Empty the Dependabot file
    type NUL >%script_dir%/%dependabot_file%

    :: Insert standard code and text in the beginning of the file
    >> %script_dir%/%dependabot_file% (
        echo # Basic dependabot.yml file with
        echo # minimum configuration for nuget
        echo # https://docs.github.com/en/free-pro-team@latest/github/administering-a-repository/keeping-your-dependencies-updated-automatically
        echo.
        echo version: 2
        echo updates:
    )
goto :eof

:: Goes to the parent directory and does a recursive loop over all csproj files
:run
    cd %parent_dir%
    :: Calls "insertSection" with the file directory for each csproj file found
    :: /R = recursive, %%f = the variable containing the result, %%~dpf = the drive and path for the result variable f
    for /R %%f in (*.csproj) do call :insertSection "%%~dpf"
    cd %script_dir%
goto :eof

:: Fetch the relative path to a csproj file
:extractRelativeProjectDir
    :: Assign the input parameter to input_path
    set input_path=%~1

    :: Change the path to be relative instead of absolute by removing the drive and everything up to the solution dir
    call set proj_path=%%input_path:%parent_dir%=%%

    :: Remove the slash at the end of the path
    set proj_path=%proj_path:~0,-1%

    :: Change all backslashes to slashes and assign the output from that to the relativeDir variable as a return value
    call set relativeDir=%%proj_path:\=/%%
goto :eof

:: Assigns the script directory and parent directory to variables
:setDirVariables
    set "script_dir=%~dp0"
    pushd %script_dir%..
    set "parent_dir=%cd%"
goto :eof

:: Inserts a project block in the dependabot file.
:insertSection
    set "relativeDir="
    call :extractRelativeProjectDir %~1

    >> %script_dir%/%dependabot_file% (
      echo   - package-ecosystem: "nuget"
      echo     directory: "%relativeDir%"
      echo     schedule:
      echo       interval: "weekly"
      echo.
    )
goto :eof