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
      setError('Vui lòng chọn một tệp hình ảnh');
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
      setError(error.response?.data?.error || 'Đã xảy ra lỗi khi xác minh xe');
    } finally {
      setVerifying(false);
    }
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    if (!vehicleId) {
      setError('Vui lòng nhập mã xe');
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
      setError(error.response?.data?.error || 'Đã xảy ra lỗi khi đăng ký xe ra bãi');
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
      <h1>Đăng ký xe ra bãi</h1>

      {successMessage && (
        <Alert variant="success" className="mb-4" dismissible onClose={() => setSuccessMessage(null)}>
          <Alert.Heading>Đăng ký xe ra bãi thành công!</Alert.Heading>
          <p>
            Xe có biển số <strong>{successMessage.licensePlate}</strong> (Mã: {successMessage.vehicleId}) đã được đăng ký ra bãi lúc {successMessage.exitTime}.
            <br />
            Thời gian đỗ xe: {successMessage.parkingDuration}
            {successMessage.isMonthlyRegistered && (
              <>
                <br />
                <span className="badge bg-success mt-2">Xe đăng ký tháng - Tự động đăng ký ra</span>
              </>
            )}
          </p>
        </Alert>
      )}

      {step === 'verify' && (
        <div className="mb-4">
          <p className="lead">
            Bước 1: Chụp ảnh xe để xác định xe cần đăng ký ra bãi.
          </p>
          <Card className="mb-4">
            <Card.Header as="h5">Xác minh xe</Card.Header>
            <Card.Body>
              <Form onSubmit={handleVerifyVehicle}>
                <Form.Group className="mb-3">
                  <Form.Label>Tải lên hình ảnh xe</Form.Label>
                  <Form.Control
                    type="file"
                    accept="image/*"
                    onChange={handleFileChange}
                    ref={fileInputRef}
                  />
                  <Form.Text className="text-muted">
                    Tải lên hình ảnh rõ nét của xe hiển thị biển số.
                  </Form.Text>
                </Form.Group>

                {previewUrl && (
                  <div className="mb-3 text-center">
                    <Image
                      src={previewUrl}
                      alt="Xem trước hình ảnh xe"
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
                        {' '}Đang xác minh...
                      </>
                    ) : 'Xác minh xe'}
                  </Button>
                  <Button
                    variant="outline-secondary"
                    onClick={switchToManualSelection}
                  >
                    Không nhận diện được? Chọn thủ công
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
              ? "Chọn thủ công: Xác nhận thông tin xe và hoàn tất đăng ký ra bãi."
              : "Bước 2: Xác nhận thông tin xe và hoàn tất đăng ký ra bãi."}
          </p>
          <Card className="mb-4">
            <Card.Header as="h5">Kết quả xác minh xe</Card.Header>
            <Card.Body>
              <Row>
                {!verificationResult.manualSelection && (
                  <Col md={6}>
                    <h6>Thông tin nhận diện:</h6>
                    <p>
                      <strong>Biển số xe:</strong> {verificationResult.recognizedLicensePlate}<br />
                      <strong>Loại xe:</strong> {verificationResult.recognizedVehicleType === 'CAR' ? 'Ô tô' : 'Xe máy'}<br />
                      {verificationResult.debugImage && (
                        <a
                          href={`/DebugFrames/${verificationResult.debugImage}`}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="btn btn-sm btn-outline-primary mt-2"
                        >
                          Xem ảnh đã chụp
                        </a>
                      )}
                    </p>
                  </Col>
                )}
                <Col md={verificationResult.manualSelection ? 12 : 6}>
                  <h6>Thông tin lưu trữ:</h6>
                  <p>
                    <strong>Mã xe:</strong> {verificationResult.vehicle.vehicleId}<br />
                    <strong>Biển số xe:</strong> {verificationResult.vehicle.licensePlate}<br />
                    <strong>Loại xe:</strong> {verificationResult.vehicle.vehicleType === 'CAR' ? 'Ô tô' : 'Xe máy'}<br />
                    <strong>Vị trí đỗ:</strong> {verificationResult.vehicle.slotId}<br />
                    <strong>Thời gian vào:</strong> {new Date(verificationResult.vehicle.entryTime).toLocaleString()}<br />
                    <strong>Loại đăng ký:</strong> {verificationResult.vehicle.isMonthlyRegistered ?
                      <span className="text-success">Đăng ký tháng (Tự động đăng ký ra)</span> :
                      <span>Vãng lai (Yêu cầu thanh toán)</span>}<br />
                  </p>
                </Col>
              </Row>

              {!verificationResult.manualSelection && (
                <Alert variant={verificationResult.vehicleTypeMatches ? "success" : "warning"}>
                  {verificationResult.vehicleTypeMatches
                    ? "✅ Loại xe khớp với thông tin đã lưu."
                    : "⚠️ Loại xe không khớp với thông tin đã lưu. Vui lòng xác minh thủ công."}
                </Alert>
              )}

              {verificationResult.manualSelection && (
                <Alert variant="info">
                  <strong>Chọn thủ công:</strong> Bạn đã chọn xe này thủ công để đăng ký ra bãi.
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
                        {' '}Đang xử lý đăng ký ra...
                      </>
                    ) : 'Xác nhận & Đăng ký xe ra'}
                  </Button>
                  <Button
                    variant="outline-secondary"
                    onClick={resetVerification}
                  >
                    Thử ảnh khác
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
            Đăng ký xe ra bãi thành công
            {result.isMonthlyRegistered && " (Xe đăng ký tháng)"}
          </Card.Header>
          <Card.Body>
            <Card.Title>{result.vehicle.licensePlate}</Card.Title>
            {result.isMonthlyRegistered && (
              <Alert variant="success" className="mb-3">
                <strong>Xe đăng ký tháng:</strong> Xe này đã được tự động đăng ký ra bãi mà không cần thanh toán.
              </Alert>
            )}
            <Card.Text>
              <strong>Mã xe:</strong> {result.vehicle.vehicleId}<br />
              <strong>Loại xe:</strong> {result.vehicle.vehicleType === 'CAR' ? 'Ô tô' : 'Xe máy'}<br />
              <strong>Vị trí đỗ:</strong> {result.vehicle.slotId}<br />
              <strong>Thời gian vào:</strong> {new Date(result.vehicle.entryTime).toLocaleString()}<br />
              <strong>Thời gian ra:</strong> {new Date(result.vehicle.exitTime).toLocaleString()}<br />
              <strong>Thời gian đỗ xe:</strong> {result.parkingDuration || 'N/A'}<br />
              <strong>Trạng thái:</strong> {result.vehicle.status === 'EXITED' ? 'Đã ra bãi' : result.vehicle.status}<br />
              <strong>Loại đăng ký:</strong> {result.isMonthlyRegistered ?
                <span className="text-success">Đăng ký tháng</span> :
                <span>Vãng lai</span>}
            </Card.Text>
          </Card.Body>
        </Card>
      )}

      {step === 'select' && (
        <div className="mb-4">
          <p className="lead">
            Chọn thủ công: Chọn một xe từ danh sách dưới đây.
          </p>
          <Alert variant="info">
            <strong>Lưu ý:</strong> Đây là phương án dự phòng khi nhận diện ảnh thất bại.
            <Button
              variant="link"
              className="p-0 ms-2"
              onClick={resetVerification}
            >
              Quay lại nhận diện ảnh
            </Button>
          </Alert>
          <h2 className="mt-4">Xe đang đỗ trong bãi</h2>
        </div>
      )}
      {fetchingVehicles ? (
        <div className="text-center">
          <Spinner animation="border" role="status">
            <span className="visually-hidden">Đang tải...</span>
          </Spinner>
        </div>
      ) : step === 'select' && parkedVehicles.length === 0 ? (
        <Alert variant="info">
          Không có xe nào đang đỗ trong bãi.
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
                    <strong>Mã xe:</strong> {vehicle.vehicleId}<br />
                    <strong>Loại xe:</strong> {vehicle.vehicleType === 'CAR' ? 'Ô tô' : 'Xe máy'}<br />
                    <strong>Vị trí đỗ:</strong> {vehicle.slotId}<br />
                    <strong>Thời gian vào:</strong> {new Date(vehicle.entryTime).toLocaleString()}<br />
                    {vehicle.isMonthlyRegistered && (
                      <span className="badge bg-success mt-2">Đăng ký tháng</span>
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
                    Chọn
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
