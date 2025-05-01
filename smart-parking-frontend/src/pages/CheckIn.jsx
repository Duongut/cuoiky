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
      setError('Vui lòng chọn một tệp hình ảnh');
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

      setError(error.response?.data?.error || 'Đã xảy ra lỗi khi đăng ký xe vào bãi');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <h1>Đăng ký xe vào bãi</h1>

      <Form onSubmit={handleSubmit}>
        <Form.Group className="mb-3">
          <Form.Label>Tải lên hình ảnh xe</Form.Label>
          <Form.Control
            type="file"
            accept="image/*"
            onChange={handleFileChange}
          />
          <Form.Text className="text-muted">
            Tải lên hình ảnh xe với biển số rõ ràng.
          </Form.Text>
        </Form.Group>

        {previewUrl && (
          <div className="mb-3">
            <p>Xem trước:</p>
            <img
              src={previewUrl}
              alt="Xem trước"
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
              {' '}Đang xử lý...
            </>
          ) : 'Đăng ký xe vào'}
        </Button>
      </Form>

      {error && (
        <Alert variant="danger" className="mt-3">
          {error}
        </Alert>
      )}

      {duplicateVehicle && (
        <Card className="mt-3 vehicle-info border-warning">
          <Card.Header as="h5" className="bg-warning text-dark">Xe đã được đăng ký vào bãi</Card.Header>
          <Card.Body>
            <Card.Title>{duplicateVehicle.licensePlate}</Card.Title>
            <Row>
              <Col md={6}>
                <Card.Text>
                  <strong>Mã xe:</strong> {duplicateVehicle.vehicleId}<br />
                  <strong>Loại xe:</strong> {duplicateVehicle.vehicleType === 'CAR' ? 'Ô tô' : 'Xe máy'}<br />
                  <strong>Vị trí đỗ:</strong> {duplicateVehicle.slotId}<br />
                  <strong>Thời gian vào:</strong> {new Date(duplicateVehicle.entryTime).toLocaleString()}<br />
                  <strong>Trạng thái:</strong> {duplicateVehicle.status === 'PARKING' ? 'Đang đỗ' : duplicateVehicle.status}
                </Card.Text>
              </Col>
              <Col md={6}>
                <Alert variant="info">
                  <p className="mb-0">
                    <strong>Lưu ý:</strong> Xe này đã được đăng ký vào bãi.
                    Bạn không thể đăng ký cùng một xe hai lần.
                  </p>
                </Alert>
                <div className="d-grid gap-2">
                  <Button
                    variant="outline-primary"
                    href="/checkout"
                  >
                    Đến trang đăng ký xe ra
                  </Button>
                </div>
              </Col>
            </Row>
          </Card.Body>
        </Card>
      )}

      {result && (
        <Card className="mt-3 vehicle-info">
          <Card.Header as="h5">Đăng ký xe vào bãi thành công</Card.Header>
          <Card.Body>
            <Card.Title>{result.vehicle.licensePlate}</Card.Title>
            <Card.Text>
              <strong>Mã xe:</strong> {result.vehicle.vehicleId}<br />
              <strong>Loại xe:</strong> {result.vehicle.vehicleType === 'CAR' ? 'Ô tô' : 'Xe máy'}<br />
              <strong>Vị trí đỗ:</strong> {result.vehicle.slotId}<br />
              <strong>Thời gian vào:</strong> {new Date(result.vehicle.entryTime).toLocaleString()}<br />
              <strong>Trạng thái:</strong> {result.vehicle.status === 'PARKING' ? 'Đang đỗ' : result.vehicle.status}
            </Card.Text>
          </Card.Body>
        </Card>
      )}
    </div>
  );
};

export default CheckIn;
