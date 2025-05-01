import React, { useEffect, useState } from 'react';
import { Container, Card, Alert, Spinner, Button } from 'react-bootstrap';
import { useNavigate, useLocation } from 'react-router-dom';
import axios from 'axios';

const MomoPaymentReturn = () => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(false);
  const [transaction, setTransaction] = useState(null);
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    const verifyPayment = async () => {
      try {
        // Parse query parameters
        const params = new URLSearchParams(location.search);
        const orderId = params.get('orderId');
        const resultCode = params.get('resultCode');

        if (!orderId) {
          setError('Missing order ID in the return URL');
          setLoading(false);
          return;
        }

        if (resultCode !== '0') {
          setError(`Payment failed with code: ${resultCode}`);
          setLoading(false);
          return;
        }

        // Get transaction details
        const response = await axios.get(`/api/payment/transactions/${orderId}`);
        setTransaction(response.data);

        // Check if payment is completed
        if (response.data.status === 'COMPLETED') {
          setSuccess(true);
        } else {
          // Wait for webhook to process the payment
          setTimeout(async () => {
            try {
              const refreshResponse = await axios.get(`/api/payment/transactions/${orderId}`);
              setTransaction(refreshResponse.data);
              
              if (refreshResponse.data.status === 'COMPLETED') {
                setSuccess(true);
              } else {
                setError('Payment verification is taking longer than expected. Please check your transaction history.');
              }
            } catch (refreshError) {
              setError('Error refreshing payment status: ' + refreshError.message);
            } finally {
              setLoading(false);
            }
          }, 5000); // Wait 5 seconds for webhook to process
          return;
        }

        setLoading(false);
      } catch (error) {
        console.error('Payment verification error:', error);
        setError(error.response?.data?.error || 'An error occurred during payment verification');
        setLoading(false);
      }
    };

    verifyPayment();
  }, [location.search]);

  return (
    <Container className="py-5">
      <Card className="shadow">
        <Card.Header as="h4" className="text-center">
          Momo Payment Result
        </Card.Header>
        <Card.Body className="text-center">
          {loading ? (
            <div className="text-center py-5">
              <Spinner animation="border" variant="primary" />
              <p className="mt-3">Verifying your payment...</p>
            </div>
          ) : success ? (
            <>
              <div className="mb-4">
                <i className="bi bi-check-circle-fill text-success" style={{ fontSize: '4rem' }}></i>
              </div>
              <h4 className="text-success mb-4">Payment Successful!</h4>
              {transaction && (
                <div className="text-start mb-4">
                  <p><strong>Transaction ID:</strong> {transaction.transactionId}</p>
                  <p><strong>Amount:</strong> {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(transaction.amount)}</p>
                  <p><strong>Date:</strong> {new Date(transaction.timestamp).toLocaleString()}</p>
                  <p><strong>Status:</strong> {transaction.status}</p>
                </div>
              )}
            </>
          ) : (
            <>
              <div className="mb-4">
                <i className="bi bi-x-circle-fill text-danger" style={{ fontSize: '4rem' }}></i>
              </div>
              <h4 className="text-danger mb-4">Payment Failed</h4>
              <Alert variant="danger">{error}</Alert>
            </>
          )}
          
          <div className="mt-4">
            <Button variant="primary" onClick={() => navigate('/dashboard')}>
              Return to Dashboard
            </Button>
          </div>
        </Card.Body>
      </Card>
    </Container>
  );
};

export default MomoPaymentReturn;
