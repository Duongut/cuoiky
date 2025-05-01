import React, { useState } from 'react';
import { Form, Button, Card, Alert, Spinner, Row, Col } from 'react-bootstrap';
import axios from 'axios';

const CheckIn = () => {
  const [selectedFile, setSelectedFile] = useState(null);
  const [previewUrl, setPreviewUrl] = useState(null);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);
  const [duplicateVehicle, setDuplicateVehicle] = useState(null);

  const handleFileChange = (event) => {
    const file = event.target.files[0];
    if (file) {
      setSelectedFile(file);
      setPreviewUrl(URL.createObjectURL(file));
      setResult(null);
      setError(null);
      setDuplicateVehicle(null);
    }
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    if (!selectedFile) {
      setError('Please select an image file');
      return;
    }

    setLoading(true);
    setError(null);
    setResult(null);

    const formData = new FormData();
    formData.append('image', selectedFile);

    try {
      const response = await axios.post('/api/vehicle/checkin', formData, {
        headers: {
          'Content-Type': 'multipart/form-data'
        }
      });

      setResult(response.data);
      setDuplicateVehicle(null);
    } catch (error) {
      console.error('Error checking in vehicle:', error);

      // Check if this is a duplicate license plate error
      if (error.response?.data?.existingVehicle) {
        setDuplicateVehicle(error.response.data.existingVehicle);
      }

      setError(error.response?.data?.error || 'An error occurred while checking in the vehicle');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <h1>Vehicle Check-In</h1>

      <Form onSubmit={handleSubmit}>
        <Form.Group className="mb-3">
          <Form.Label>Upload Vehicle Image</Form.Label>
          <Form.Control
            type="file"
            accept="image/*"
            onChange={handleFileChange}
          />
          <Form.Text className="text-muted">
            Upload an image of the vehicle showing the license plate clearly.
          </Form.Text>
        </Form.Group>

        {previewUrl && (
          <div className="mb-3">
            <p>Preview:</p>
            <img
              src={previewUrl}
              alt="Preview"
              style={{ maxWidth: '100%', maxHeight: '300px' }}
              className="img-thumbnail"
            />
          </div>
        )}

        <Button
          variant="primary"
          type="submit"
          disabled={!selectedFile || loading}
        >
          {loading ? (
            <>
              <Spinner
                as="span"
                animation="border"
                size="sm"
                role="status"
                aria-hidden="true"
              />
              {' '}Processing...
            </>
          ) : 'Check In Vehicle'}
        </Button>
      </Form>

      {error && (
        <Alert variant="danger" className="mt-3">
          {error}
        </Alert>
      )}

      {duplicateVehicle && (
        <Card className="mt-3 vehicle-info border-warning">
          <Card.Header as="h5" className="bg-warning text-dark">Vehicle Already Parked</Card.Header>
          <Card.Body>
            <Card.Title>{duplicateVehicle.licensePlate}</Card.Title>
            <Row>
              <Col md={6}>
                <Card.Text>
                  <strong>Vehicle ID:</strong> {duplicateVehicle.vehicleId}<br />
                  <strong>Vehicle Type:</strong> {duplicateVehicle.vehicleType}<br />
                  <strong>Assigned Slot:</strong> {duplicateVehicle.slotId}<br />
                  <strong>Entry Time:</strong> {new Date(duplicateVehicle.entryTime).toLocaleString()}<br />
                  <strong>Status:</strong> {duplicateVehicle.status}
                </Card.Text>
              </Col>
              <Col md={6}>
                <Alert variant="info">
                  <p className="mb-0">
                    <strong>Note:</strong> This vehicle is already parked in the system.
                    You cannot check in the same vehicle twice.
                  </p>
                </Alert>
                <div className="d-grid gap-2">
                  <Button
                    variant="outline-primary"
                    href="/checkout"
                  >
                    Go to Check-Out
                  </Button>
                </div>
              </Col>
            </Row>
          </Card.Body>
        </Card>
      )}

      {result && (
        <Card className="mt-3 vehicle-info">
          <Card.Header as="h5">Vehicle Checked In Successfully</Card.Header>
          <Card.Body>
            <Card.Title>{result.vehicle.licensePlate}</Card.Title>
            <Card.Text>
              <strong>Vehicle ID:</strong> {result.vehicle.vehicleId}<br />
              <strong>Vehicle Type:</strong> {result.vehicle.vehicleType}<br />
              <strong>Assigned Slot:</strong> {result.vehicle.slotId}<br />
              <strong>Entry Time:</strong> {new Date(result.vehicle.entryTime).toLocaleString()}<br />
              <strong>Status:</strong> {result.vehicle.status}
            </Card.Text>
          </Card.Body>
        </Card>
      )}
    </div>
  );
};

export default CheckIn;
