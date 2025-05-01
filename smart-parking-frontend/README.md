# Smart Parking Frontend

This is the frontend for the Smart Parking System, built with React and Vite.

## Features

- Dashboard with real-time parking slot status
- Vehicle check-in with license plate recognition
- Vehicle check-out
- Real-time updates using SignalR

## Prerequisites

- Node.js 16.x or higher
- npm 8.x or higher

## Installation

```bash
# Install dependencies
npm install
```

## Development

```bash
# Start the development server
npm run dev
```

The application will be available at http://localhost:3000.

## Building for Production

```bash
# Build the application
npm run build
```

The built application will be in the `dist` directory.

## Project Structure

- `src/components`: Reusable UI components
- `src/pages`: Page components
- `src/services`: API services
- `src/assets`: Static assets like images

## API Integration

The frontend communicates with the backend API at http://localhost:5125. The API endpoints used are:

- `GET /api/parking/slots`: Get all parking slots
- `GET /api/parking/vehicles/parked`: Get all parked vehicles
- `POST /api/vehicle/checkin`: Check in a vehicle
- `POST /api/vehicle/checkout/{vehicleId}`: Check out a vehicle

## Real-time Updates

The application uses SignalR for real-time updates. The SignalR hub is at `/parkingHub` and provides the following events:

- `ReceiveParkingUpdate`: When a parking slot status changes
- `ReceiveVehicleUpdate`: When a vehicle status changes
