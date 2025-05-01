import React, { useState, useEffect } from 'react';
import { Container, Card, Button, Spinner, Alert } from 'react-bootstrap';
import { useParams, useLocation, useNavigate } from 'react-router-dom';
import axios from 'axios';
import { API_URL } from '../config';

const CheckoutComplete = () => {
  const { vehicleId } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(false);
  const [vehicle, setVehicle] = useState(null);

  useEffect(() => {
    if (!location.state || !location.state.transactionId) {
      setError('No transaction information provided');
      return;
    }

    completeCheckout();
  }, [location, vehicleId]);

  const completeCheckout = async () => {
    try {
      setLoading(true);
      setError(null);

      const response = await axios.post(`${API_URL}/api/vehicle/checkout/${vehicleId}`, {
        transactionId: location.state.transactionId,
        paymentConfirmed: true
      });

      // Store the vehicle data
      setVehicle(response.data.vehicle);

      // Calculate parking duration if not provided by the backend
      if (!response.data.parkingDuration && response.data.vehicle.entryTime && response.data.vehicle.exitTime) {
        const parkingDuration = calculateParkingDuration(
          response.data.vehicle.entryTime,
          response.data.vehicle.exitTime
        );

        // Store the calculated parking duration in the location state
        if (location.state) {
          location.state.parkingDuration = parkingDuration;
        }
      }

      setSuccess(true);
      setLoading(false);
    } catch (err) {
      setLoading(false);
      if (err.response && err.response.data && err.response.data.error) {
        setError(err.response.data.error);
      } else {
        setError('Failed to complete checkout process');
      }
      console.error('Error completing checkout:', err);
    }
  };

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-GB', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const calculateParkingDuration = (entryTimeStr, exitTimeStr) => {
    const entryTime = new Date(entryTimeStr);
    const exitTime = new Date(exitTimeStr);
    const durationMs = exitTime - entryTime;

    // Convert to days, hours, minutes, seconds
    const days = Math.floor(durationMs / (1000 * 60 * 60 * 24));
    const hours = Math.floor((durationMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    const minutes = Math.floor((durationMs % (1000 * 60 * 60)) / (1000 * 60));
    const seconds = Math.floor((durationMs % (1000 * 60)) / 1000);

    if (days > 0) {
      return `${days}d ${hours}h ${minutes}m`;
    } else if (hours > 0) {
      return `${hours}h ${minutes}m`;
    } else {
      return `${minutes}m ${seconds}s`;
    }
  };

  if (loading) {
    return (
      <Container className="py-5 text-center">
        <Spinner animation="border" />
        <p>Completing checkout process...</p>
      </Container>
    );
  }

  if (error) {
    return (
      <Container className="py-5">
        <Card className="p-4 text-center">
          <div className="mb-4">
            <i className="bi bi-exclamation-triangle-fill text-warning" style={{ fontSize: '5rem' }}></i>
          </div>
          <h3>Checkout Error</h3>
          <Alert variant="danger" className="mt-3">
            {error}
          </Alert>
          <div className="mt-4">
            <Button variant="primary" onClick={() => navigate('/checkout')}>
              Return to Checkout
            </Button>
          </div>
        </Card>
      </Container>
    );
  }

  if (success && vehicle) {
    return (
      <Container className="py-5">
        <Card className="p-4">
          <div className="text-center mb-4">
            <div className="success-icon">
              <i className="bi bi-check-circle-fill text-success" style={{ fontSize: '5rem' }}></i>
            </div>
            <h3 className="mt-3 text-success">Checkout Successful!</h3>
          </div>

          <div className="vehicle-details">
            <div className="row mb-3">
              <div className="col-md-4 fw-bold">Vehicle ID:</div>
              <div className="col-md-8">{vehicle.vehicleId}</div>
            </div>
            <div className="row mb-3">
              <div className="col-md-4 fw-bold">License Plate:</div>
              <div className="col-md-8">{vehicle.licensePlate}</div>
            </div>
            <div className="row mb-3">
              <div className="col-md-4 fw-bold">Vehicle Type:</div>
              <div className="col-md-8">{vehicle.vehicleType}</div>
            </div>
            <div className="row mb-3">
              <div className="col-md-4 fw-bold">Parking Slot:</div>
              <div className="col-md-8">{vehicle.slotId}</div>
            </div>
            <div className="row mb-3">
              <div className="col-md-4 fw-bold">Entry Time:</div>
              <div className="col-md-8">{formatDate(vehicle.entryTime)}</div>
            </div>
            <div className="row mb-3">
              <div className="col-md-4 fw-bold">Exit Time:</div>
              <div className="col-md-8">{formatDate(vehicle.exitTime)}</div>
            </div>
            <div className="row mb-3">
              <div className="col-md-4 fw-bold">Parking Duration:</div>
              <div className="col-md-8">{location.state?.parkingDuration || calculateParkingDuration(vehicle.entryTime, vehicle.exitTime)}</div>
            </div>
          </div>

          <div className="d-flex justify-content-center mt-4">
            <Button
              variant="primary"
              onClick={() => navigate('/')}
            >
              Return to Home
            </Button>
          </div>
        </Card>
      </Container>
    );
  }

  return (
    <Container className="py-5 text-center">
      <Card className="p-4">
        <h3>Processing checkout...</h3>
        <p>Please wait while we complete your checkout process.</p>
      </Card>
    </Container>
  );
};

export default CheckoutComplete;
