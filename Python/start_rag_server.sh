#!/bin/bash
# Script to run RAG API Server

echo "========================================"
echo "Starting RAG API Server"
echo "========================================"
echo ""
echo "Server will run on: http://localhost:8000"
echo "API Document: http://localhost:8000/docs"
echo ""
echo "Press CTRL+C to stop the server"
echo ""

cd "$(dirname "$0")"
python rag_api_server.py
