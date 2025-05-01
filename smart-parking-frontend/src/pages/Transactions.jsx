import React, { useState, useEffect } from 'react';
import { Table, Card, Badge, Button, Form, Row, Col, Alert, Spinner, Modal } from 'react-bootstrap';
import DatePicker from 'react-datepicker';
import axios from 'axios';
import { FaFileInvoice, FaSearch, FaFilter, FaDownload } from 'react-icons/fa';
import 'react-datepicker/dist/react-datepicker.css';

const Transactions = () => {
  const [transactions, setTransactions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [startDate, setStartDate] = useState(new Date(new Date().setDate(new Date().getDate() - 30)));
  const [endDate, setEndDate] = useState(new Date());
  const [filterType, setFilterType] = useState('ALL');
  const [filterMethod, setFilterMethod] = useState('ALL');
  const [searchTerm, setSearchTerm] = useState('');
  const [showInvoiceModal, setShowInvoiceModal] = useState(false);
  const [selectedTransaction, setSelectedTransaction] = useState(null);
  const [generatingInvoice, setGeneratingInvoice] = useState(false);
  const [invoiceUrl, setInvoiceUrl] = useState(null);
  const [invoiceError, setInvoiceError] = useState(null);
  const [revenueStats, setRevenueStats] = useState(null);

  useEffect(() => {
    fetchTransactions();
    fetchRevenueStats();
  }, []);

  const fetchTransactions = async () => {
    try {
      setLoading(true);
      const response = await axios.get(`/api/payment/transactions/date-range?startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`);
      setTransactions(response.data);
      setLoading(false);
    } catch (err) {
      setError('Failed to load transactions. Please try again.');
      setLoading(false);
      console.error('Error fetching transactions:', err);
    }
  };

  const fetchRevenueStats = async () => {
    try {
      const byMethod = await axios.get(`/api/payment/revenue/by-method?startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`);
      const byType = await axios.get(`/api/payment/revenue/by-type?startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`);
      
      setRevenueStats({
        byMethod: byMethod.data,
        byType: byType.data
      });
    } catch (err) {
      console.error('Error fetching revenue stats:', err);
    }
  };

  const handleSearch = () => {
    fetchTransactions();
    fetchRevenueStats();
  };

  const handleGenerateInvoice = async (transaction) => {
    setSelectedTransaction(transaction);
    setGeneratingInvoice(true);
    setInvoiceUrl(null);
    setInvoiceError(null);
    setShowInvoiceModal(true);
    
    try {
      // Check if invoice already exists
      if (transaction.invoiceUrl) {
        setInvoiceUrl(transaction.invoiceUrl);
        setGeneratingInvoice(false);
        return;
      }
      
      // Generate new invoice
      const response = await axios.post(`/api/payment/invoice/generate/${transaction.id}`);
      setInvoiceUrl(response.data.invoiceUrl);
      
      // Update transaction in the list with the new invoice URL
      setTransactions(prevTransactions => 
        prevTransactions.map(t => 
          t.id === transaction.id ? { ...t, invoiceUrl: response.data.invoiceUrl } : t
        )
      );
    } catch (err) {
      setInvoiceError('Failed to generate invoice. Please try again.');
      console.error('Error generating invoice:', err);
    } finally {
      setGeneratingInvoice(false);
    }
  };

  const getStatusBadge = (status) => {
    switch (status) {
      case 'COMPLETED':
        return <Badge bg="success">Completed</Badge>;
      case 'PENDING':
        return <Badge bg="warning">Pending</Badge>;
      case 'FAILED':
        return <Badge bg="danger">Failed</Badge>;
      case 'REFUNDED':
        return <Badge bg="info">Refunded</Badge>;
      default:
        return <Badge bg="secondary">{status}</Badge>;
    }
  };

  const getPaymentMethodBadge = (method) => {
    switch (method) {
      case 'CASH':
        return <Badge bg="success">Cash</Badge>;
      case 'STRIPE':
        return <Badge bg="primary">Stripe</Badge>;
      case 'MOMO':
        return <Badge bg="danger">MoMo</Badge>;
      default:
        return <Badge bg="secondary">{method}</Badge>;
    }
  };

  const formatDate = (dateString) => {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleString();
  };

  const filteredTransactions = transactions.filter(transaction => {
    // Filter by type
    if (filterType !== 'ALL' && transaction.type !== filterType) {
      return false;
    }
    
    // Filter by payment method
    if (filterMethod !== 'ALL' && transaction.paymentMethod !== filterMethod) {
      return false;
    }
    
    // Search by vehicle ID or description
    if (searchTerm && !transaction.vehicleId.toLowerCase().includes(searchTerm.toLowerCase()) && 
        !transaction.description.toLowerCase().includes(searchTerm.toLowerCase())) {
      return false;
    }
    
    return true;
  });

  return (
    <div className="transactions">
      <h2 className="mb-4">Transaction Management</h2>
      
      {error && <Alert variant="danger">{error}</Alert>}
      
      <Card className="mb-4">
        <Card.Header>
          <FaFilter className="me-2" />
          Filters & Search
        </Card.Header>
        <Card.Body>
          <Row>
            <Col md={3}>
              <Form.Group className="mb-3">
                <Form.Label>Start Date</Form.Label>
                <DatePicker
                  selected={startDate}
                  onChange={date => setStartDate(date)}
                  selectsStart
                  startDate={startDate}
                  endDate={endDate}
                  className="form-control"
                  dateFormat="yyyy-MM-dd"
                />
              </Form.Group>
            </Col>
            <Col md={3}>
              <Form.Group className="mb-3">
                <Form.Label>End Date</Form.Label>
                <DatePicker
                  selected={endDate}
                  onChange={date => setEndDate(date)}
                  selectsEnd
                  startDate={startDate}
                  endDate={endDate}
                  minDate={startDate}
                  className="form-control"
                  dateFormat="yyyy-MM-dd"
                />
              </Form.Group>
            </Col>
            <Col md={2}>
              <Form.Group className="mb-3">
                <Form.Label>Transaction Type</Form.Label>
                <Form.Select
                  value={filterType}
                  onChange={e => setFilterType(e.target.value)}
                >
                  <option value="ALL">All Types</option>
                  <option value="MONTHLY">Monthly</option>
                  <option value="CASUAL">Casual</option>
                </Form.Select>
              </Form.Group>
            </Col>
            <Col md={2}>
              <Form.Group className="mb-3">
                <Form.Label>Payment Method</Form.Label>
                <Form.Select
                  value={filterMethod}
                  onChange={e => setFilterMethod(e.target.value)}
                >
                  <option value="ALL">All Methods</option>
                  <option value="CASH">Cash</option>
                  <option value="STRIPE">Stripe</option>
                  <option value="MOMO">MoMo</option>
                </Form.Select>
              </Form.Group>
            </Col>
            <Col md={2}>
              <Form.Group className="mb-3">
                <Form.Label>Search</Form.Label>
                <div className="d-flex">
                  <Form.Control
                    type="text"
                    placeholder="Vehicle ID or Description"
                    value={searchTerm}
                    onChange={e => setSearchTerm(e.target.value)}
                  />
                </div>
              </Form.Group>
            </Col>
          </Row>
          <div className="d-flex justify-content-end">
            <Button variant="primary" onClick={handleSearch}>
              <FaSearch className="me-2" />
              Apply Filters
            </Button>
          </div>
        </Card.Body>
      </Card>
      
      {revenueStats && (
        <Card className="mb-4">
          <Card.Header>Revenue Summary</Card.Header>
          <Card.Body>
            <Row>
              <Col md={6}>
                <h5>By Payment Method</h5>
                <Table striped bordered>
                  <thead>
                    <tr>
                      <th>Payment Method</th>
                      <th>Amount (VND)</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td>Cash</td>
                      <td>{revenueStats.byMethod.CASH?.toLocaleString() || 0}</td>
                    </tr>
                    <tr>
                      <td>Stripe</td>
                      <td>{revenueStats.byMethod.STRIPE?.toLocaleString() || 0}</td>
                    </tr>
                    <tr>
                      <td>MoMo</td>
                      <td>{revenueStats.byMethod.MOMO?.toLocaleString() || 0}</td>
                    </tr>
                    <tr className="table-primary">
                      <td><strong>Total</strong></td>
                      <td><strong>{revenueStats.byMethod.TOTAL?.toLocaleString() || 0}</strong></td>
                    </tr>
                  </tbody>
                </Table>
              </Col>
              <Col md={6}>
                <h5>By Transaction Type</h5>
                <Table striped bordered>
                  <thead>
                    <tr>
                      <th>Transaction Type</th>
                      <th>Amount (VND)</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td>Monthly Subscriptions</td>
                      <td>{revenueStats.byType.MONTHLY?.toLocaleString() || 0}</td>
                    </tr>
                    <tr>
                      <td>Casual Parking</td>
                      <td>{revenueStats.byType.CASUAL?.toLocaleString() || 0}</td>
                    </tr>
                    <tr className="table-primary">
                      <td><strong>Total</strong></td>
                      <td><strong>{revenueStats.byType.TOTAL?.toLocaleString() || 0}</strong></td>
                    </tr>
                  </tbody>
                </Table>
              </Col>
            </Row>
          </Card.Body>
        </Card>
      )}
      
      <Card>
        <Card.Header>Transaction History</Card.Header>
        <Card.Body>
          {loading ? (
            <div className="text-center my-4">
              <Spinner animation="border" role="status">
                <span className="visually-hidden">Loading...</span>
              </Spinner>
            </div>
          ) : filteredTransactions.length === 0 ? (
            <Alert variant="info">No transactions found for the selected filters.</Alert>
          ) : (
            <Table responsive striped hover>
              <thead>
                <tr>
                  <th>Transaction ID</th>
                  <th>Vehicle ID</th>
                  <th>Amount</th>
                  <th>Type</th>
                  <th>Payment Method</th>
                  <th>Status</th>
                  <th>Date & Time</th>
                  <th>Description</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredTransactions.map(transaction => (
                  <tr key={transaction.id}>
                    <td>{transaction.id.substring(0, 8)}...</td>
                    <td>{transaction.vehicleId}</td>
                    <td>{transaction.amount.toLocaleString()} VND</td>
                    <td>{transaction.type}</td>
                    <td>{getPaymentMethodBadge(transaction.paymentMethod)}</td>
                    <td>{getStatusBadge(transaction.status)}</td>
                    <td>{formatDate(transaction.timestamp)}</td>
                    <td>{transaction.description}</td>
                    <td>
                      <Button 
                        variant="outline-primary" 
                        size="sm"
                        onClick={() => handleGenerateInvoice(transaction)}
                        disabled={transaction.status !== 'COMPLETED'}
                      >
                        <FaFileInvoice className="me-1" />
                        Invoice
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </Table>
          )}
        </Card.Body>
      </Card>

      {/* Invoice Modal */}
      <Modal show={showInvoiceModal} onHide={() => setShowInvoiceModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>Transaction Invoice</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {generatingInvoice ? (
            <div className="text-center my-4">
              <Spinner animation="border" role="status">
                <span className="visually-hidden">Generating invoice...</span>
              </Spinner>
              <p className="mt-2">Generating invoice...</p>
            </div>
          ) : invoiceError ? (
            <Alert variant="danger">{invoiceError}</Alert>
          ) : invoiceUrl ? (
            <div className="text-center">
              <p>Invoice has been generated successfully!</p>
              <div className="d-grid gap-2 mt-3">
                <Button 
                  variant="primary" 
                  href={invoiceUrl} 
                  target="_blank"
                >
                  <FaDownload className="me-2" />
                  Download Invoice
                </Button>
              </div>
            </div>
          ) : (
            <Alert variant="info">Preparing invoice...</Alert>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowInvoiceModal(false)}>
            Close
          </Button>
        </Modal.Footer>
      </Modal>
    </div>
  );
};

export default Transactions;
