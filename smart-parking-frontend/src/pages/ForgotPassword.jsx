import React, { useState } from 'react';
import { Container, Row, Col, Card, Form, Button, Alert, Spinner } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import { FaEnvelope, FaParking, FaArrowLeft } from 'react-icons/fa';
import axios from 'axios';
import { toast } from 'react-toastify';

const ForgotPassword = () => {
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      await axios.post('/api/auth/forgot-password', { email });
      setSuccess(true);
      toast.success('Password reset instructions sent to your email');
    } catch (err) {
      console.error('Forgot password error:', err);
      setError(err.response?.data?.error || 'An error occurred. Please try again.');
      toast.error('Failed to process password reset request');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container className="d-flex align-items-center justify-content-center" style={{ minHeight: '100vh' }}>
      <Row className="w-100">
        <Col md={6} className="mx-auto">
          <Card className="shadow-lg border-0 rounded-lg">
            <Card.Header className="bg-primary text-white text-center py-4">
              <div className="d-flex justify-content-center align-items-center mb-2">
                <FaParking size={30} className="me-2" />
                <h3 className="mb-0">Smart Parking System</h3>
              </div>
              <p className="mb-0">Forgot Password</p>
            </Card.Header>
            <Card.Body className="p-4">
              {error && <Alert variant="danger">{error}</Alert>}
              
              {success ? (
                <Alert variant="success">
                  <p>Password reset instructions have been sent to your email.</p>
                  <p>Please check your inbox and follow the instructions to reset your password.</p>
                  <p className="mb-0">If you don't receive an email within a few minutes, please check your spam folder.</p>
                </Alert>
              ) : (
                <>
                  <p className="text-muted mb-4">
                    Enter your email address and we'll send you instructions to reset your password.
                    <br />
                    <small>Note: This feature is only available for admin accounts.</small>
                  </p>
                  <Form onSubmit={handleSubmit}>
                    <Form.Group className="mb-4">
                      <Form.Label>
                        <FaEnvelope className="me-2" />
                        Email Address
                      </Form.Label>
                      <Form.Control
                        type="email"
                        placeholder="Enter your email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        required
                        disabled={loading}
                      />
                    </Form.Group>

                    <div className="d-grid">
                      <Button
                        variant="primary"
                        type="submit"
                        disabled={loading}
                        className="py-2"
                      >
                        {loading ? (
                          <>
                            <Spinner
                              as="span"
                              animation="border"
                              size="sm"
                              role="status"
                              aria-hidden="true"
                              className="me-2"
                            />
                            Sending...
                          </>
                        ) : (
                          'Send Reset Instructions'
                        )}
                      </Button>
                    </div>
                  </Form>
                </>
              )}
            </Card.Body>
            <Card.Footer className="text-center py-3">
              <div className="small">
                <Link to="/login" className="text-decoration-none">
                  <FaArrowLeft className="me-1" /> Back to Login
                </Link>
              </div>
            </Card.Footer>
          </Card>
        </Col>
      </Row>
    </Container>
  );
};

export default ForgotPassword;
