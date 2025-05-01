import React, { useState, useEffect } from 'react';
import { Table, Button, Card, Badge, Modal, Form, Alert, Spinner } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import axios from 'axios';
import DatePicker from 'react-datepicker';
import 'react-datepicker/dist/react-datepicker.css';

const MonthlyVehicles = () => {
  const [vehicles, setVehicles] = useState([]);
  const [subscriptions, setSubscriptions] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showRenewalModal, setShowRenewalModal] = useState(false);
  const [selectedVehicle, setSelectedVehicle] = useState(null);
  const [renewalMonths, setRenewalMonths] = useState(1);
  const [calculatedFee, setCalculatedFee] = useState(null);
  const [renewalSuccess, setRenewalSuccess] = useState(false);
  const [renewalError, setRenewalError] = useState(null);
  const [renewalLoading, setRenewalLoading] = useState(false);
  const [expiringVehicles, setExpiringVehicles] = useState([]);
  const [showExpiringModal, setShowExpiringModal] = useState(false);

  useEffect(() => {
    fetchMonthlyVehicles();
    fetchExpiringSubscriptions();
  }, []);

  const fetchMonthlyVehicles = async () => {
    try {
      setLoading(true);
      console.log('Fetching monthly vehicles...');
      const response = await axios.get('/api/monthlyvehicle');
      console.log('Monthly vehicles response:', response.data);
      setVehicles(response.data);

      // Fetch subscription details for each vehicle
      const subscriptionData = {};
      for (const vehicle of response.data) {
        if (vehicle.subscriptionId) {
          try {
            const subResponse = await axios.get(`/api/subscription/${vehicle.subscriptionId}`);
            subscriptionData[vehicle.id] = subResponse.data;
          } catch (err) {
            console.error(`Error fetching subscription for vehicle ${vehicle.id}:`, err);
          }
        }
      }
      setSubscriptions(subscriptionData);
      setLoading(false);
    } catch (err) {
      setError('Failed to load monthly vehicles. Please try again.');
      setLoading(false);
      console.error('Error fetching monthly vehicles:', err);
    }
  };

  const fetchExpiringSubscriptions = async () => {
    try {
      const response = await axios.get('/api/monthlyvehicle/check-expiring?days=7');
      setExpiringVehicles(response.data);
    } catch (err) {
      console.error('Error fetching expiring subscriptions:', err);
    }
  };

  const handleRenewalClick = (vehicle) => {
    setSelectedVehicle(vehicle);
    setRenewalMonths(1);
    setCalculatedFee(null);
    setRenewalSuccess(false);
    setRenewalError(null);
    setShowRenewalModal(true);
    calculateRenewalFee(vehicle.vehicleType, 1);
  };

  const calculateRenewalFee = async (vehicleType, months) => {
    try {
      const response = await axios.get(`/api/monthlyvehicle/calculate-fee?vehicleType=${vehicleType}&months=${months}`);
      setCalculatedFee(response.data.fee);
    } catch (err) {
      console.error('Error calculating renewal fee:', err);
    }
  };

  const handleRenewalSubmit = async () => {
    if (!selectedVehicle) return;

    setRenewalLoading(true);
    setRenewalError(null);

    try {
      await axios.post(`/api/monthlyvehicle/${selectedVehicle.id}/renew`, {
        months: renewalMonths
      });

      setRenewalSuccess(true);
      setRenewalLoading(false);

      // Refresh data after successful renewal
      fetchMonthlyVehicles();
      fetchExpiringSubscriptions();

      // Close modal after a delay
      setTimeout(() => {
        setShowRenewalModal(false);
      }, 2000);
    } catch (err) {
      setRenewalError('Failed to renew subscription. Please try again.');
      setRenewalLoading(false);
      console.error('Error renewing subscription:', err);
    }
  };

  const getStatusBadge = (status) => {
    switch (status) {
      case 'ACTIVE':
        return <Badge bg="success">Active</Badge>;
      case 'EXPIRED':
        return <Badge bg="danger">Expired</Badge>;
      case 'CANCELLED':
        return <Badge bg="warning">Cancelled</Badge>;
      default:
        return <Badge bg="secondary">Unknown</Badge>;
    }
  };

  const formatDate = (dateString) => {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleDateString();
  };

  const getDaysRemaining = (endDate) => {
    if (!endDate) return 0;
    const end = new Date(endDate);
    const now = new Date();
    const diffTime = end - now;
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays > 0 ? diffDays : 0;
  };

  return (
    <div className="monthly-vehicles">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Monthly Vehicles Management</h2>
        <div>
          <Button
            variant="warning"
            className="me-2"
            onClick={() => setShowExpiringModal(true)}
            disabled={expiringVehicles.length === 0}
          >
            Expiring Soon ({expiringVehicles.length})
          </Button>
          <Button
            variant="primary"
            as={Link}
            to="/monthly-registration"
          >
            Register New Vehicle
          </Button>
        </div>
      </div>

      {error && <Alert variant="danger">{error}</Alert>}

      <Card>
        <Card.Header>Monthly Vehicles</Card.Header>
        <Card.Body>
          {loading ? (
            <div className="text-center my-4">
              <Spinner animation="border" role="status">
                <span className="visually-hidden">Loading...</span>
              </Spinner>
            </div>
          ) : vehicles.length === 0 ? (
            <Alert variant="info">No monthly vehicles registered yet.</Alert>
          ) : (
            <Table responsive striped hover>
              <thead>
                <tr>
                  <th>Vehicle ID</th>
                  <th>License Plate</th>
                  <th>Type</th>
                  <th>Owner</th>
                  <th>Status</th>
                  <th>Expiry Date</th>
                  <th>Days Left</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {vehicles.map(vehicle => {
                  const subscription = subscriptions[vehicle.id];
                  return (
                    <tr key={vehicle.id}>
                      <td>{vehicle.vehicleId}</td>
                      <td>{vehicle.licensePlate}</td>
                      <td>{vehicle.vehicleType === 'CAR' ? 'Car' : 'Motorbike'}</td>
                      <td>
                        <div>{vehicle.ownerInfo?.name}</div>
                        <small className="text-muted">{vehicle.ownerInfo?.phone}</small>
                      </td>
                      <td>{subscription ? getStatusBadge(subscription.status) : 'N/A'}</td>
                      <td>{subscription ? formatDate(subscription.endDate) : 'N/A'}</td>
                      <td>
                        {subscription ? getDaysRemaining(subscription.endDate) : 'N/A'}
                      </td>
                      <td>
                        <Button
                          variant="outline-primary"
                          size="sm"
                          onClick={() => handleRenewalClick(vehicle)}
                        >
                          Renew
                        </Button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </Table>
          )}
        </Card.Body>
      </Card>

      {/* Renewal Modal */}
      <Modal show={showRenewalModal} onHide={() => setShowRenewalModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Renew Subscription</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {renewalSuccess ? (
            <Alert variant="success">
              Subscription renewed successfully!
            </Alert>
          ) : (
            <>
              {renewalError && <Alert variant="danger">{renewalError}</Alert>}

              {selectedVehicle && (
                <div className="mb-3">
                  <p><strong>Vehicle ID:</strong> {selectedVehicle.vehicleId}</p>
                  <p><strong>License Plate:</strong> {selectedVehicle.licensePlate}</p>
                  <p><strong>Owner:</strong> {selectedVehicle.ownerInfo?.name}</p>
                </div>
              )}

              <Form.Group className="mb-3">
                <Form.Label>Renewal Duration (months)</Form.Label>
                <Form.Control
                  type="number"
                  min="1"
                  max="24"
                  value={renewalMonths}
                  onChange={(e) => {
                    const months = parseInt(e.target.value);
                    setRenewalMonths(months);
                    if (selectedVehicle) {
                      calculateRenewalFee(selectedVehicle.vehicleType, months);
                    }
                  }}
                />
                <Form.Text className="text-muted">
                  Discounts: 10% for 3+ months, 20% for 6+ months, 30% for 12+ months
                </Form.Text>
              </Form.Group>

              {calculatedFee && (
                <Alert variant="info">
                  <strong>Renewal Fee:</strong> {calculatedFee.toLocaleString()} VND
                </Alert>
              )}
            </>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowRenewalModal(false)}>
            Close
          </Button>
          {!renewalSuccess && (
            <Button
              variant="primary"
              onClick={handleRenewalSubmit}
              disabled={renewalLoading}
            >
              {renewalLoading ? 'Processing...' : 'Confirm Renewal'}
            </Button>
          )}
        </Modal.Footer>
      </Modal>

      {/* Expiring Subscriptions Modal */}
      <Modal
        show={showExpiringModal}
        onHide={() => setShowExpiringModal(false)}
        size="lg"
      >
        <Modal.Header closeButton>
          <Modal.Title>Subscriptions Expiring Soon</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {expiringVehicles.length === 0 ? (
            <Alert variant="info">No subscriptions expiring in the next 7 days.</Alert>
          ) : (
            <Table responsive striped>
              <thead>
                <tr>
                  <th>Vehicle ID</th>
                  <th>License Plate</th>
                  <th>Owner</th>
                  <th>Expiry Date</th>
                  <th>Days Left</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {expiringVehicles.map(vehicle => (
                  <tr key={vehicle.vehicleId}>
                    <td>{vehicle.vehicleId}</td>
                    <td>{vehicle.licensePlate}</td>
                    <td>
                      <div>{vehicle.ownerName}</div>
                      <small className="text-muted">{vehicle.ownerPhone}</small>
                    </td>
                    <td>{formatDate(vehicle.expiryDate)}</td>
                    <td>
                      <Badge bg={vehicle.daysRemaining <= 3 ? "danger" : "warning"}>
                        {vehicle.daysRemaining} days
                      </Badge>
                    </td>
                    <td>
                      <Button
                        variant="outline-primary"
                        size="sm"
                        onClick={() => {
                          setShowExpiringModal(false);
                          // Find the full vehicle object to pass to renewal
                          const fullVehicle = vehicles.find(v => v.vehicleId === vehicle.vehicleId);
                          if (fullVehicle) {
                            handleRenewalClick(fullVehicle);
                          }
                        }}
                      >
                        Renew
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </Table>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowExpiringModal(false)}>
            Close
          </Button>
        </Modal.Footer>
      </Modal>
    </div>
  );
};

export default MonthlyVehicles;
