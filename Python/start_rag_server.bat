@echo off
REM Script to run RAG API Server
REM Make sure you're in the Python directory

cd /d %~dp0

echo ========================================
echo Starting RAG API Server
echo ========================================
echo.
echo Server will run on: http://localhost:8000
echo API Document: http://localhost:8000/docs
echo.
echo Press CTRL+C to stop the server
echo.

python rag_api_server.py

pause
