#!/bin/bash

# Activate the virtual environment if it exists
if [ -d ".venv" ]; then
    source .venv/bin/activate
fi

# Install Flask if not already installed
pip install flask

# Start the API
python api.py
