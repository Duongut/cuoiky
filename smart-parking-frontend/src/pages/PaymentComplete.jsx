import React, { useEffect, useState } from 'react';
import { Card, Button, Alert } from 'react-bootstrap';
import { useLocation, useNavigate } from 'react-router-dom';
import { FaCheckCircle, FaFileInvoice, FaHome } from 'react-icons/fa';
import axios from 'axios';

const PaymentComplete = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const [paymentData, setPaymentData] = useState(null);
  const [invoiceUrl, setInvoiceUrl] = useState(null);
  const [generatingInvoice, setGeneratingInvoice] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    // Get payment data from location state
    if (location.state?.transactionId) {
      setPaymentData(location.state);

      // Generate invoice for any completed payment with a transaction ID
      if (location.state.transactionId !== 'ONLINE_PAYMENT') {
        generateInvoice(location.state.transactionId);
      }
    } else {
      setError('No payment information available.');
    }
  }, [location]);

  const generateInvoice = async (transactionId) => {
    if (transactionId === 'ONLINE_PAYMENT') return;

    setGeneratingInvoice(true);
    try {
      const response = await axios.post(`/api/payment/invoice/generate/${transactionId}`);
      setInvoiceUrl(response.data.invoiceUrl);
    } catch (err) {
      console.error('Error generating invoice:', err);
    } finally {
      setGeneratingInvoice(false);
    }
  };

  const handleGoHome = () => {
    navigate('/');
  };

  if (!paymentData) {
    return (
      <div className="payment-complete">
        <h2 className="mb-4">Payment Complete</h2>
        {error && <Alert variant="danger">{error}</Alert>}
        <Button variant="primary" onClick={handleGoHome}>
          <FaHome className="me-2" />
          Go to Dashboard
        </Button>
      </div>
    );
  }

  return (
    <div className="payment-complete">
      <div className="text-center mb-4">
        <FaCheckCircle size={80} className="text-success mb-3" />
        <h2>Payment Completed Successfully!</h2>
      </div>

      <Card className="mb-4">
        <Card.Header>Payment Details</Card.Header>
        <Card.Body>
          <div className="row">
            <div className="col-md-6">
              <p><strong>Transaction ID:</strong> {paymentData.transactionId}</p>
              <p><strong>Vehicle ID:</strong> {paymentData.vehicleId}</p>
            </div>
            <div className="col-md-6">
              <p><strong>Amount:</strong> {paymentData.amount?.toLocaleString()} VND</p>
              <p><strong>Payment Method:</strong> {paymentData.paymentMethod}</p>
            </div>
          </div>

          <div className="mt-4">
            {generatingInvoice ? (
              <div className="d-grid gap-2">
                <Button variant="outline-primary" disabled>
                  <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                  Generating Invoice...
                </Button>
              </div>
            ) : invoiceUrl ? (
              <div className="d-grid gap-2">
                <Button
                  variant="primary"
                  href={invoiceUrl}
                  target="_blank"
                  size="lg"
                >
                  <FaFileInvoice className="me-2" />
                  Download Invoice
                </Button>
              </div>
            ) : (
              <div className="d-grid gap-2">
                <Button
                  variant="outline-secondary"
                  disabled
                >
                  <FaFileInvoice className="me-2" />
                  Invoice Unavailable
                </Button>
              </div>
            )}
          </div>
        </Card.Body>
      </Card>

      <div className="d-grid gap-2">
        <Button variant="primary" size="lg" onClick={handleGoHome}>
          <FaHome className="me-2" />
          Return to Dashboard
        </Button>
      </div>
    </div>
  );
};

export default PaymentComplete;
