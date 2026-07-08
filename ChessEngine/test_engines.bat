@echo off
setlocal enabledelayedexpansion

:: ==========================================
:: CONFIGURATION
:: ==========================================
set "TIME_CONTROL=5+0.08"
set "ROUNDS=1000"
:: Point this to your EPD or PGN book:
set "BOOK_FILE=.\books\8moves_v3.pgn"
set "OUTPUT_DIR=.\output"

:: 1. Check if fastchess is installed
where fastchess >nul 2>nul
if %errorlevel% neq 0 (
    echo Error: 'fastchess' command not found. Please install it first or add it to PATH.
    exit /b 1
)

:: 2. Check arguments
if "%~2"=="" (
    echo Usage: %~nx0 ^<engine_1_filename^> ^<engine_2_filename^>
    echo Example: %~nx0 chess_v1_base.exe chess_v2_move_ordering.exe
    exit /b 1
)

set "ENGINE1_PATH=%~1"
set "ENGINE2_PATH=%~2"
:: Extract file names without extensions (.exe) for cleaner logs and output filenames
set "ENGINE1_NAME=%~n1"
set "ENGINE2_NAME=%~n2"

:: 3. Verify engine files exist
if not exist "%ENGINE1_PATH%" (
    echo Error: Engine file not found: %ENGINE1_PATH%
    exit /b 1
)

if not exist "%ENGINE2_PATH%" (
    echo Error: Engine file not found: %ENGINE2_PATH%
    exit /b 1
)

:: 4. Make sure engine files have executable permissions
:: (Skipped: Not needed in Windows for local .exe files)

:: 5. Automatically detect CPU cores to optimize concurrency
set "CORES=%NUMBER_OF_PROCESSORS%"
if "%CORES%"=="" set "CORES=4"

:: 6. Automatically detect opening book format (EPD vs PGN)
set "BOOK_FORMAT=pgn"
if exist "%BOOK_FILE%" (
    :: Extract file extension (includes the dot)
    for %%I in ("%BOOK_FILE%") do set "EXTENSION=%%~xI"
    
    if /i "!EXTENSION!"==".epd" (
        set "BOOK_FORMAT=epd"
    ) else if /i "!EXTENSION!"==".pgn" (
        set "BOOK_FORMAT=pgn"
    ) else (
        echo Warning: Unknown book format '!EXTENSION!'. Defaulting to PGN.
    )
)

echo ==========================================================
echo  Starting Match: %ENGINE1_NAME% vs %ENGINE2_NAME%
echo  Time Control:   %TIME_CONTROL%
echo  Book Format:    %BOOK_FORMAT% (%BOOK_FILE%)
echo  CPU Threads:    %CORES% parallel games
echo ==========================================================

:: 7. Ensure output directory exists
if not exist "%OUTPUT_DIR%" (
    mkdir "%OUTPUT_DIR%"
)

:: 8. Configure openings flag if file exists
set "OPENING_FLAG="
if exist "%BOOK_FILE%" (
    :: Quotes are wrapped around BOOK_FILE to safely handle paths with spaces
    set "OPENING_FLAG=-openings file="%BOOK_FILE%" format=%BOOK_FORMAT% order=random"
) else (
    echo Warning: Opening book '%BOOK_FILE%' not found.
    echo Engines will play from the startpos ^(may result in draws/repetition^).
    echo.
)

:: 9. Execute fastchess with SPRT (A/B testing)
:: Note: %OPENING_FLAG% is placed at the end to prevent multiline (^) continuation 
:: breaking if the book file doesn't exist and the variable evaluates as empty.
fastchess ^
  -engine cmd="%ENGINE1_PATH%" name="%ENGINE1_NAME%" ^
  -engine cmd="%ENGINE2_PATH%" name="%ENGINE2_NAME%" ^
  -each tc="%TIME_CONTROL%" ^
  -rounds "%ROUNDS%" ^
  -repeat ^
  -concurrency "%CORES%" ^
  -recover ^
  -pgnout file="%OUTPUT_DIR%\%ENGINE1_NAME%_vs_%ENGINE2_NAME%.pgn" ^
  -sprt elo0=0 elo1=10 alpha=0.05 beta=0.05 %OPENING_FLAG%