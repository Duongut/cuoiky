import React, { useState, useEffect, useRef } from 'react';
import { Form, Button, Card, Alert, Spinner, InputGroup, Row, Col, Image } from 'react-bootstrap';
import axios from 'axios';
import PaymentModal from './PaymentModal';

const CheckOut = () => {
  const [vehicleId, setVehicleId] = useState('');
  const [parkedVehicles, setParkedVehicles] = useState([]);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState(null);
  const [successMessage, setSuccessMessage] = useState(null);
  const [error, setError] = useState(null);
  const [fetchingVehicles, setFetchingVehicles] = useState(true);
  const [selectedFile, setSelectedFile] = useState(null);
  const [previewUrl, setPreviewUrl] = useState(null);
  const [verificationResult, setVerificationResult] = useState(null);
  const [verifying, setVerifying] = useState(false);
  const [step, setStep] = useState('verify'); // 'verify', 'select', 'confirm'
  const [showPaymentModal, setShowPaymentModal] = useState(false);
  const [paymentData, setPaymentData] = useState(null);
  const fileInputRef = useRef(null);

  useEffect(() => {
    const fetchParkedVehicles = async () => {
      try {
        const response = await axios.get('/api/parking/vehicles/parked');
        setParkedVehicles(response.data);
      } catch (error) {
        console.error('Error fetching parked vehicles:', error);
      } finally {
        setFetchingVehicles(false);
      }
    };

    fetchParkedVehicles();
  }, []);

  const handleFileChange = (event) => {
    const file = event.target.files[0];
    if (file) {
      setSelectedFile(file);
      setPreviewUrl(URL.createObjectURL(file));
      setError(null);
    }
  };

  const handleVerifyVehicle = async (event) => {
    event.preventDefault();

    if (!selectedFile) {
      setError('Please select an image file');
      return;
    }

    setVerifying(true);
    setError(null);
    setVerificationResult(null);

    const formData = new FormData();
    formData.append('image', selectedFile);

    try {
      const response = await axios.post('/api/vehicle/verify-checkout', formData, {
        headers: {
          'Content-Type': 'multipart/form-data'
        }
      });

      setVerificationResult(response.data);
      setVehicleId(response.data.vehicle.vehicleId);
      setStep('confirm');
    } catch (error) {
      console.error('Error verifying vehicle:', error);
      setError(error.response?.data?.error || 'An error occurred while verifying the vehicle');
    } finally {
      setVerifying(false);
    }
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    if (!vehicleId) {
      setError('Please enter a vehicle ID');
      return;
    }

    setLoading(true);
    setError(null);
    setResult(null);

    try {
      // First, get fee calculation and payment requirement
      const response = await axios.post(`/api/vehicle/checkout/${vehicleId}`);

      // Check if this is a monthly registered vehicle
      if (response.data.isMonthlyRegistered) {
        // For monthly vehicles, process checkout directly without payment
        console.log("Monthly registered vehicle detected - automatic checkout");
        handleCheckoutComplete(response.data);
      }
      // Check if payment is required for casual vehicles
      else if (response.data.paymentRequired) {
        // Show payment modal with fee information
        setPaymentData(response.data);
        setShowPaymentModal(true);
      } else {
        // If no payment required (unlikely in this system), process checkout directly
        handleCheckoutComplete(response.data);
      }
    } catch (error) {
      console.error('Error checking out vehicle:', error);
      setError(error.response?.data?.error || 'An error occurred while checking out the vehicle');
    } finally {
      setLoading(false);
    }
  };

  const handleCheckoutComplete = (checkoutData) => {
    // Update UI with checkout result
    setResult(checkoutData);

    // Remove the vehicle from the parked vehicles list
    setParkedVehicles(prevVehicles =>
      prevVehicles.filter(v => v.vehicleId !== vehicleId)
    );

    // Show success message
    setSuccessMessage({
      licensePlate: checkoutData.vehicle.licensePlate,
      vehicleId: checkoutData.vehicle.vehicleId,
      exitTime: new Date(checkoutData.vehicle.exitTime).toLocaleString(),
      parkingDuration: checkoutData.parkingDuration || 'N/A',
      isMonthlyRegistered: checkoutData.isMonthlyRegistered || false
    });

    // Reset the verification state
    setVerificationResult(null);
    setSelectedFile(null);
    setPreviewUrl(null);
    setStep('verify');
    setShowPaymentModal(false);
    setPaymentData(null);

    // Clear success message after 10 seconds
    setTimeout(() => {
      setSuccessMessage(null);
    }, 10000);
  };

  const handleVehicleSelect = (vehicle) => {
    setVehicleId(vehicle.vehicleId);
    setError(null);
    setResult(null);

    // Create a manual verification result to show confirmation screen
    setVerificationResult({
      vehicle: vehicle,
      recognizedLicensePlate: vehicle.licensePlate,
      recognizedVehicleType: vehicle.vehicleType,
      vehicleTypeMatches: true,
      manualSelection: true
    });

    setStep('confirm');
  };

  const resetVerification = () => {
    setSelectedFile(null);
    setPreviewUrl(null);
    setVerificationResult(null);
    setStep('verify');
    setError(null);
  };

  const switchToManualSelection = () => {
    setSelectedFile(null);
    setPreviewUrl(null);
    setVerificationResult(null);
    setStep('select');
    setError(null);
  };

  return (
    <div>
      <h1>Vehicle Check-Out</h1>

      {successMessage && (
        <Alert variant="success" className="mb-4" dismissible onClose={() => setSuccessMessage(null)}>
          <Alert.Heading>Vehicle Successfully Checked Out!</Alert.Heading>
          <p>
            Vehicle with license plate <strong>{successMessage.licensePlate}</strong> (ID: {successMessage.vehicleId}) has been checked out at {successMessage.exitTime}.
            <br />
            Parking duration: {successMessage.parkingDuration}
            {successMessage.isMonthlyRegistered && (
              <>
                <br />
                <span className="badge bg-success mt-2">Monthly Registered Vehicle - Automatic Checkout</span>
              </>
            )}
          </p>
        </Alert>
      )}

      {step === 'verify' && (
        <div className="mb-4">
          <p className="lead">
            Step 1: Take a photo of the vehicle to identify it for checkout.
          </p>
          <Card className="mb-4">
            <Card.Header as="h5">Vehicle Verification</Card.Header>
            <Card.Body>
              <Form onSubmit={handleVerifyVehicle}>
                <Form.Group className="mb-3">
                  <Form.Label>Upload Vehicle Image</Form.Label>
                  <Form.Control
                    type="file"
                    accept="image/*"
                    onChange={handleFileChange}
                    ref={fileInputRef}
                  />
                  <Form.Text className="text-muted">
                    Upload a clear image of the vehicle showing its license plate.
                  </Form.Text>
                </Form.Group>

                {previewUrl && (
                  <div className="mb-3 text-center">
                    <Image
                      src={previewUrl}
                      alt="Vehicle preview"
                      style={{ maxHeight: '300px' }}
                      thumbnail
                    />
                  </div>
                )}

                <div className="d-flex gap-2">
                  <Button
                    variant="primary"
                    type="submit"
                    disabled={!selectedFile || verifying}
                  >
                    {verifying ? (
                      <>
                        <Spinner
                          as="span"
                          animation="border"
                          size="sm"
                          role="status"
                          aria-hidden="true"
                        />
                        {' '}Verifying...
                      </>
                    ) : 'Verify Vehicle'}
                  </Button>
                  <Button
                    variant="outline-secondary"
                    onClick={switchToManualSelection}
                  >
                    Can't identify? Select manually
                  </Button>
                </div>
              </Form>
            </Card.Body>
          </Card>
        </div>
      )}

      {step === 'confirm' && verificationResult && (
        <div className="mb-4">
          <p className="lead">
            {verificationResult.manualSelection
              ? "Manual Selection: Confirm vehicle identity and complete checkout."
              : "Step 2: Confirm vehicle identity and complete checkout."}
          </p>
          <Card className="mb-4">
            <Card.Header as="h5">Vehicle Verification Result</Card.Header>
            <Card.Body>
              <Row>
                {!verificationResult.manualSelection && (
                  <Col md={6}>
                    <h6>Recognized Information:</h6>
                    <p>
                      <strong>License Plate:</strong> {verificationResult.recognizedLicensePlate}<br />
                      <strong>Vehicle Type:</strong> {verificationResult.recognizedVehicleType}<br />
                      {verificationResult.debugImage && (
                        <a
                          href={`/DebugFrames/${verificationResult.debugImage}`}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="btn btn-sm btn-outline-primary mt-2"
                        >
                          View Captured Image
                        </a>
                      )}
                    </p>
                  </Col>
                )}
                <Col md={verificationResult.manualSelection ? 12 : 6}>
                  <h6>Stored Information:</h6>
                  <p>
                    <strong>Vehicle ID:</strong> {verificationResult.vehicle.vehicleId}<br />
                    <strong>License Plate:</strong> {verificationResult.vehicle.licensePlate}<br />
                    <strong>Vehicle Type:</strong> {verificationResult.vehicle.vehicleType}<br />
                    <strong>Slot:</strong> {verificationResult.vehicle.slotId}<br />
                    <strong>Entry Time:</strong> {new Date(verificationResult.vehicle.entryTime).toLocaleString()}<br />
                    <strong>Registration Type:</strong> {verificationResult.vehicle.isMonthlyRegistered ?
                      <span className="text-success">Monthly Registered (Automatic Checkout)</span> :
                      <span>Casual (Payment Required)</span>}<br />
                  </p>
                </Col>
              </Row>

              {!verificationResult.manualSelection && (
                <Alert variant={verificationResult.vehicleTypeMatches ? "success" : "warning"}>
                  {verificationResult.vehicleTypeMatches
                    ? "✅ Vehicle type matches the stored information."
                    : "⚠️ Vehicle type does not match the stored information. Please verify manually."}
                </Alert>
              )}

              {verificationResult.manualSelection && (
                <Alert variant="info">
                  <strong>Manual Selection:</strong> You have manually selected this vehicle for checkout.
                </Alert>
              )}

              <Form onSubmit={handleSubmit}>
                <Form.Group className="mb-3" hidden>
                  <Form.Control
                    type="text"
                    value={vehicleId}
                    readOnly
                  />
                </Form.Group>

                <div className="d-flex gap-2">
                  <Button
                    variant="primary"
                    type="submit"
                    disabled={loading}
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
                        {' '}Processing Checkout...
                      </>
                    ) : 'Confirm & Check Out Vehicle'}
                  </Button>
                  <Button
                    variant="outline-secondary"
                    onClick={resetVerification}
                  >
                    Try Another Photo
                  </Button>
                </div>
              </Form>
            </Card.Body>
          </Card>
        </div>
      )}

      {error && (
        <Alert variant="danger" className="mt-3">
          {error}
        </Alert>
      )}

      {result && (
        <Card className="mt-3 vehicle-info" border={result.isMonthlyRegistered ? "success" : ""}>
          <Card.Header as="h5" className={result.isMonthlyRegistered ? "bg-success text-white" : ""}>
            Vehicle Checked Out Successfully
            {result.isMonthlyRegistered && " (Monthly Registered)"}
          </Card.Header>
          <Card.Body>
            <Card.Title>{result.vehicle.licensePlate}</Card.Title>
            {result.isMonthlyRegistered && (
              <Alert variant="success" className="mb-3">
                <strong>Monthly Registered Vehicle:</strong> This vehicle has been automatically checked out without payment.
              </Alert>
            )}
            <Card.Text>
              <strong>Vehicle ID:</strong> {result.vehicle.vehicleId}<br />
              <strong>Vehicle Type:</strong> {result.vehicle.vehicleType}<br />
              <strong>Slot:</strong> {result.vehicle.slotId}<br />
              <strong>Entry Time:</strong> {new Date(result.vehicle.entryTime).toLocaleString()}<br />
              <strong>Exit Time:</strong> {new Date(result.vehicle.exitTime).toLocaleString()}<br />
              <strong>Parking Duration:</strong> {result.parkingDuration || 'N/A'}<br />
              <strong>Status:</strong> {result.vehicle.status}<br />
              <strong>Registration Type:</strong> {result.isMonthlyRegistered ?
                <span className="text-success">Monthly Registered</span> :
                <span>Casual</span>}
            </Card.Text>
          </Card.Body>
        </Card>
      )}

      {step === 'select' && (
        <div className="mb-4">
          <p className="lead">
            Manual Selection: Choose a vehicle from the list below.
          </p>
          <Alert variant="info">
            <strong>Note:</strong> This is a fallback option when photo recognition fails.
            <Button
              variant="link"
              className="p-0 ms-2"
              onClick={resetVerification}
            >
              Return to photo identification
            </Button>
          </Alert>
          <h2 className="mt-4">Currently Parked Vehicles</h2>
        </div>
      )}
      {fetchingVehicles ? (
        <div className="text-center">
          <Spinner animation="border" role="status">
            <span className="visually-hidden">Loading...</span>
          </Spinner>
        </div>
      ) : step === 'select' && parkedVehicles.length === 0 ? (
        <Alert variant="info">
          No vehicles currently parked.
        </Alert>
      ) : step === 'select' ? (
        <div className="row">
          {parkedVehicles.map(vehicle => (
            <div className="col-md-4 mb-3" key={vehicle.vehicleId}>
              <Card
                className="h-100 cursor-pointer"
                onClick={() => handleVehicleSelect(vehicle)}
                style={{ cursor: 'pointer' }}
                border={vehicleId === vehicle.vehicleId ? 'primary' : ''}
              >
                <Card.Body>
                  <Card.Title>{vehicle.licensePlate}</Card.Title>
                  <Card.Text>
                    <strong>Vehicle ID:</strong> {vehicle.vehicleId}<br />
                    <strong>Vehicle Type:</strong> {vehicle.vehicleType}<br />
                    <strong>Slot:</strong> {vehicle.slotId}<br />
                    <strong>Entry Time:</strong> {new Date(vehicle.entryTime).toLocaleString()}<br />
                    {vehicle.isMonthlyRegistered && (
                      <span className="badge bg-success mt-2">Monthly Registered</span>
                    )}
                  </Card.Text>
                  <Button
                    variant="outline-primary"
                    size="sm"
                    onClick={(e) => {
                      e.stopPropagation();
                      handleVehicleSelect(vehicle);
                    }}
                  >
                    Select
                  </Button>
                </Card.Body>
              </Card>
            </div>
          ))}
        </div>
      ) : null}
      {/* Payment Modal */}
      {showPaymentModal && paymentData && (
        <PaymentModal
          show={showPaymentModal}
          onHide={() => {
            setShowPaymentModal(false);
            setPaymentData(null);
          }}
          vehicleData={paymentData}
          onPaymentComplete={handleCheckoutComplete}
        />
      )}
    </div>
  );
};

export default CheckOut;
