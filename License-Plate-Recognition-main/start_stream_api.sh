#!/bin/bash

# Activate the virtual environment if it exists
if [ -d ".venv" ]; then
    source .venv/bin/activate
fi

# Install required packages
pip install flask flask-cors

# Start the streaming API
python stream_api.py
