import React, { useState, useEffect } from 'react';
import { Container, Row, Col, Card, Alert, Button, Badge, Spinner } from 'react-bootstrap';
import WebcamViewer from '../components/WebcamViewer';
import axios from 'axios';
import * as signalR from '@microsoft/signalr';

const CameraMonitoring = () => {
  const [cameras, setCameras] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [recentDetections, setRecentDetections] = useState([]);
  const [connection, setConnection] = useState(null);

  useEffect(() => {
    // Fetch cameras
    fetchCameras();

    // Set up SignalR connection
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('/parkingHub')
      .withAutomaticReconnect()
      .build();

    newConnection.on('ReceiveVehicleEntry', (data) => {
      setRecentDetections(prev => [
        {
          type: 'ENTRY',
          timestamp: new Date(),
          ...data
        },
        ...prev.slice(0, 9) // Keep only the 10 most recent detections
      ]);
    });

    newConnection.on('ReceiveVehicleExit', (data) => {
      setRecentDetections(prev => [
        {
          type: 'EXIT',
          timestamp: new Date(),
          ...data
        },
        ...prev.slice(0, 9) // Keep only the 10 most recent detections
      ]);
    });

    newConnection.on('ReceiveManualSnapshot', (data) => {
      setRecentDetections(prev => [
        {
          type: 'SNAPSHOT',
          timestamp: new Date(),
          ...data
        },
        ...prev.slice(0, 9) // Keep only the 10 most recent detections
      ]);
    });

    newConnection.start()
      .then(() => {
        console.log('SignalR Connected');
        setConnection(newConnection);
      })
      .catch(err => {
        console.error('SignalR Connection Error: ', err);
        setError('Failed to connect to real-time updates');
      });

    return () => {
      if (connection) {
        connection.stop();
      }
    };
  }, []);

  const fetchCameras = async () => {
    try {
      setLoading(true);
      const response = await axios.get('/api/cameras');
      setCameras(response.data.cameras || []);
      setError(null);
    } catch (error) {
      console.error('Error fetching cameras:', error);
      setError('Failed to load cameras. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleDetection = (cameraId, detection) => {
    console.log(`Detection from camera ${cameraId}:`, detection);

    // Add the detection to the recent detections list with proper timestamp
    setRecentDetections(prev => [
      {
        type: 'SNAPSHOT',
        timestamp: new Date(),
        cameraId: cameraId,
        ...detection
      },
      ...prev.slice(0, 9) // Keep only the 10 most recent detections
    ]);
  };

  const handleProcessVehicle = async (cameraId, vehicleData) => {
    try {
      setLoading(true);
      setError(null);

      console.log(`Processing vehicle for camera ${cameraId}:`, vehicleData);

      const response = await axios.post(`/api/cameras/${cameraId}/process-vehicle`, vehicleData);

      // Show success message
      const actionText = vehicleData.action === 'checkin' ? 'checked in' : 'checked out';
      alert(`Vehicle ${vehicleData.licensePlate} successfully ${actionText}`);

      console.log('Process vehicle response:', response.data);
    } catch (error) {
      console.error(`Error processing vehicle for camera ${cameraId}:`, error);
      setError(error.response?.data?.error || error.message);
      alert(`Error: ${error.response?.data?.error || error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const setupDefaultCameras = async () => {
    try {
      setLoading(true);

      // Start entry cameras
      await axios.post('/api/cameras/IN-01/start', { cameraIndex: 0 });
      await axios.post('/api/cameras/IN-02/start', { cameraIndex: 0 });

      // Start exit cameras
      await axios.post('/api/cameras/OUT-01/start', { cameraIndex: 0 });
      await axios.post('/api/cameras/OUT-02/start', { cameraIndex: 0 });

      // Refresh camera list
      await fetchCameras();

      setError(null);
    } catch (error) {
      console.error('Error setting up default cameras:', error);
      setError('Failed to set up default cameras. Please check if your webcam is connected.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container fluid>
      <h1 className="mb-4">Camera Monitoring</h1>

      {error && (
        <Alert variant="danger" className="mb-4">
          {error}
        </Alert>
      )}

      {cameras.length === 0 && !loading && (
        <div className="text-center mb-4">
          <p>No cameras are currently set up.</p>
          <Button variant="primary" onClick={setupDefaultCameras}>
            Setup Default Cameras
          </Button>
        </div>
      )}

      <Row>
        <Col md={8}>
          <h2 className="mb-3">Live Feeds</h2>
          <Row>
            <Col md={6}>
              <WebcamViewer
                cameraId="IN-01"
                title="Entry Camera 1"
                onDetection={(detection) => handleDetection('IN-01', detection)}
                onProcessVehicle={(vehicleData) => handleProcessVehicle('IN-01', vehicleData)}
              />
            </Col>
            <Col md={6}>
              <WebcamViewer
                cameraId="OUT-01"
                title="Exit Camera 1"
                onDetection={(detection) => handleDetection('OUT-01', detection)}
                onProcessVehicle={(vehicleData) => handleProcessVehicle('OUT-01', vehicleData)}
              />
            </Col>
          </Row>
        </Col>

        <Col md={4}>
          <h2 className="mb-3">Recent Detections</h2>
          <div className="detection-list">
            {recentDetections.length === 0 ? (
              <Alert variant="info">
                No recent detections. Vehicles will appear here when detected by cameras.
              </Alert>
            ) : (
              recentDetections.map((detection, index) => (
                <Card key={index} className="mb-2 detection-card">
                  <Card.Body className="py-2">
                    <div className="d-flex justify-content-between align-items-center">
                      <div>
                        <strong>{detection.licensePlate}</strong>
                        <span className="ms-2">
                          {detection.type === 'ENTRY' ? (
                            <Badge bg="success">Entry</Badge>
                          ) : detection.type === 'EXIT' ? (
                            <Badge bg="danger">Exit</Badge>
                          ) : (
                            <Badge bg="info">Snapshot</Badge>
                          )}
                        </span>
                      </div>
                      <small className="text-muted">
                        {detection.timestamp instanceof Date
                          ? detection.timestamp.toLocaleTimeString()
                          : typeof detection.timestamp === 'string'
                            ? new Date(detection.timestamp).toLocaleTimeString()
                            : new Date().toLocaleTimeString()}
                      </small>
                    </div>
                    <div className="small mt-1">
                      <div>Vehicle ID: {detection.vehicleId}</div>
                      <div>Type: {detection.vehicleType}</div>
                      <div>Slot: {detection.slotId}</div>
                      <div>Camera: {detection.cameraId}</div>
                      {(detection.type === 'ENTRY' && detection.classificationMethod) && (
                        <div>
                          Classification: {detection.classificationMethod === 'ml' ? 'ML Model' :
                                          detection.classificationMethod === 'format' ? 'License Format' : 'Fallback'}
                          {detection.classificationMethod === 'ml' && (
                            <span className="ms-1 text-muted">
                              (Confidence: {(detection.classificationConfidence * 100).toFixed(1)}%)
                            </span>
                          )}
                        </div>
                      )}
                      {detection.type === 'SNAPSHOT' && (
                        <div>
                          Classification: ML Model
                          <span className="ms-1 text-muted">
                            (Confidence: {(detection.confidence * 100).toFixed(1)}%)
                          </span>
                        </div>
                      )}
                      {detection.type === 'EXIT' && detection.parkingDuration && (
                        <div>Duration: {detection.parkingDuration}</div>
                      )}
                      {detection.debugImage && (
                        <div className="mt-1">
                          <a href={`/DebugFrames/${detection.debugImage}`} target="_blank" rel="noopener noreferrer" className="text-primary">
                            View Image
                          </a>
                        </div>
                      )}
                    </div>
                  </Card.Body>
                </Card>
              ))
            )}
          </div>
        </Col>
      </Row>
    </Container>
  );
};

export default CameraMonitoring;
