import React, { useState, useEffect } from 'react';
import { Container, Row, Col, Card, Form, Button, Alert, Spinner, Table, Tabs, Tab, Modal, Badge } from 'react-bootstrap';
import { FaCog, FaMoneyBillWave, FaPercentage, FaSave, FaUndo, FaParking, FaExclamationTriangle, FaSync, FaUsers, FaUserPlus, FaUserEdit, FaTrash, FaKey, FaCheck, FaTimes } from 'react-icons/fa';
import axios from 'axios';
import { toast } from 'react-toastify';

const Settings = () => {
  const [activeTab, setActiveTab] = useState('parking-fees');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(null);

  // Parking fee settings
  const [parkingFees, setParkingFees] = useState({
    casualMotorbikeFee: 0,
    casualCarFee: 0,
    monthlyMotorbikeFee: 0,
    monthlyCarFee: 0
  });

  // Original values for reset
  const [originalParkingFees, setOriginalParkingFees] = useState({});

  // Discount settings
  const [discountSettings, setDiscountSettings] = useState({
    discountTiers: []
  });

  // Original values for reset
  const [originalDiscountSettings, setOriginalDiscountSettings] = useState({});

  // Parking space settings
  const [parkingSpaces, setParkingSpaces] = useState({
    motorcycleSlots: 0,
    carSlots: 0,
    zones: []
  });

  // Original values for reset
  const [originalParkingSpaces, setOriginalParkingSpaces] = useState({});

  // Current parking configuration
  const [parkingConfiguration, setParkingConfiguration] = useState({
    configuredMotorcycleSlots: 0,
    configuredCarSlots: 0,
    actualMotorcycleSlots: 0,
    actualCarSlots: 0,
    occupiedMotorcycleSlots: 0,
    occupiedCarSlots: 0
  });

  // Reset confirmation modal
  const [showResetModal, setShowResetModal] = useState(false);

  // Employee management state
  const [employees, setEmployees] = useState([]);
  const [showEmployeeModal, setShowEmployeeModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [showResetPasswordModal, setShowResetPasswordModal] = useState(false);
  const [currentEmployee, setCurrentEmployee] = useState(null);
  const [employeeForm, setEmployeeForm] = useState({
    username: '',
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    phoneNumber: '',
    role: 'STAFF'
  });
  const [newPassword, setNewPassword] = useState('');
  const [loadingEmployees, setLoadingEmployees] = useState(false);

  useEffect(() => {
    fetchSettings();
    fetchParkingConfiguration();
    fetchEmployees();
  }, []);

  const fetchSettings = async () => {
    setLoading(true);
    setError(null);

    try {
      // Fetch parking fee settings
      const parkingFeesResponse = await axios.get('/api/settings/parking-fees');
      setParkingFees(parkingFeesResponse.data);
      setOriginalParkingFees(parkingFeesResponse.data);

      // Fetch discount settings
      const discountResponse = await axios.get('/api/settings/discounts');
      setDiscountSettings(discountResponse.data);
      setOriginalDiscountSettings(discountResponse.data);

      // Fetch parking space settings
      const parkingSpacesResponse = await axios.get('/api/settings/parking-spaces');
      setParkingSpaces(parkingSpacesResponse.data);
      setOriginalParkingSpaces(parkingSpacesResponse.data);
    } catch (err) {
      console.error('Error fetching settings:', err);
      setError('Failed to load settings. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const fetchParkingConfiguration = async () => {
    try {
      const response = await axios.get('/api/parking/configuration');
      setParkingConfiguration(response.data);
    } catch (err) {
      console.error('Error fetching parking configuration:', err);
      // Don't set error state here to avoid blocking the UI
      toast.error('Failed to load current parking configuration');
    }
  };

  const handleParkingFeeChange = (e) => {
    const { name, value } = e.target;
    setParkingFees({
      ...parkingFees,
      [name]: parseFloat(value) || 0
    });
  };

  const handleDiscountTierChange = (index, field, value) => {
    const updatedTiers = [...discountSettings.discountTiers];
    updatedTiers[index] = {
      ...updatedTiers[index],
      [field]: field === 'discountPercentage' ? parseInt(value) || 0 : parseInt(value) || 0
    };

    setDiscountSettings({
      ...discountSettings,
      discountTiers: updatedTiers
    });
  };

  const addDiscountTier = () => {
    const newTier = {
      minMonths: 1,
      maxMonths: 2,
      discountPercentage: 0
    };

    setDiscountSettings({
      ...discountSettings,
      discountTiers: [...discountSettings.discountTiers, newTier]
    });
  };

  const removeDiscountTier = (index) => {
    const updatedTiers = [...discountSettings.discountTiers];
    updatedTiers.splice(index, 1);

    setDiscountSettings({
      ...discountSettings,
      discountTiers: updatedTiers
    });
  };

  const resetParkingFees = () => {
    setParkingFees(originalParkingFees);
    toast.info('Parking fee settings reset to original values');
  };

  const resetDiscountSettings = () => {
    setDiscountSettings(originalDiscountSettings);
    toast.info('Discount settings reset to original values');
  };

  const saveParkingFees = async () => {
    setSaving(true);
    setError(null);
    setSuccess(null);

    try {
      await axios.put('/api/settings/parking-fees', parkingFees);
      setSuccess('Parking fee settings saved successfully');
      setOriginalParkingFees(parkingFees);
      toast.success('Parking fee settings saved successfully');
    } catch (err) {
      console.error('Error saving parking fee settings:', err);
      setError(err.response?.data?.error || 'Failed to save parking fee settings');
      toast.error('Failed to save parking fee settings');
    } finally {
      setSaving(false);
    }
  };

  const saveDiscountSettings = async () => {
    setSaving(true);
    setError(null);
    setSuccess(null);

    try {
      // Sort discount tiers by min months
      const sortedTiers = [...discountSettings.discountTiers].sort((a, b) => a.minMonths - b.minMonths);

      // Validate tiers
      for (let i = 0; i < sortedTiers.length - 1; i++) {
        if (sortedTiers[i].maxMonths >= sortedTiers[i + 1].minMonths) {
          throw new Error('Discount tier ranges must not overlap');
        }
      }

      await axios.put('/api/settings/discounts', {
        ...discountSettings,
        discountTiers: sortedTiers
      });

      setSuccess('Discount settings saved successfully');
      setOriginalDiscountSettings({
        ...discountSettings,
        discountTiers: sortedTiers
      });
      setDiscountSettings({
        ...discountSettings,
        discountTiers: sortedTiers
      });
      toast.success('Discount settings saved successfully');
    } catch (err) {
      console.error('Error saving discount settings:', err);
      setError(err.response?.data?.error || err.message || 'Failed to save discount settings');
      toast.error('Failed to save discount settings');
    } finally {
      setSaving(false);
    }
  };

  const handleParkingSpaceChange = (e) => {
    const { name, value } = e.target;
    setParkingSpaces({
      ...parkingSpaces,
      [name]: parseInt(value) || 0
    });
  };

  const resetParkingSpaces = () => {
    setParkingSpaces(originalParkingSpaces);
    toast.info('Parking space settings reset to original values');
  };

  const saveParkingSpaces = async () => {
    setSaving(true);
    setError(null);
    setSuccess(null);

    try {
      // Validate
      if (parkingSpaces.motorcycleSlots < 0) {
        throw new Error('Number of motorcycle slots cannot be negative');
      }

      if (parkingSpaces.carSlots < 0) {
        throw new Error('Number of car slots cannot be negative');
      }

      // Check if we're reducing slots
      if (parkingSpaces.motorcycleSlots < parkingConfiguration.actualMotorcycleSlots ||
          parkingSpaces.carSlots < parkingConfiguration.actualCarSlots) {

        // Check if there are occupied slots
        if (parkingConfiguration.occupiedMotorcycleSlots > 0 &&
            parkingSpaces.motorcycleSlots < parkingConfiguration.actualMotorcycleSlots) {
          throw new Error('Cannot reduce motorcycle slots while there are vehicles parked. Please empty the parking lot first.');
        }

        if (parkingConfiguration.occupiedCarSlots > 0 &&
            parkingSpaces.carSlots < parkingConfiguration.actualCarSlots) {
          throw new Error('Cannot reduce car slots while there are vehicles parked. Please empty the parking lot first.');
        }
      }

      // Save settings first
      await axios.put('/api/settings/parking-spaces', parkingSpaces);

      // Then adjust actual parking spaces
      const result = await axios.put('/api/parking/spaces', {
        motorcycleSlots: parkingSpaces.motorcycleSlots,
        carSlots: parkingSpaces.carSlots
      });

      setSuccess('Parking space settings saved and applied successfully');
      setOriginalParkingSpaces(parkingSpaces);

      // Refresh parking configuration
      await fetchParkingConfiguration();

      toast.success('Parking space settings saved and applied successfully');
    } catch (err) {
      console.error('Error saving parking space settings:', err);
      setError(err.response?.data?.error || err.message || 'Failed to save parking space settings');
      toast.error('Failed to save parking space settings');
    } finally {
      setSaving(false);
    }
  };

  const handleResetParkingLot = async () => {
    setSaving(true);
    setError(null);
    setSuccess(null);

    try {
      const result = await axios.post('/api/parking/reset');

      setSuccess('Parking lot reset successfully');

      // Refresh parking configuration
      await fetchParkingConfiguration();

      toast.success('Parking lot reset successfully');
      setShowResetModal(false);
    } catch (err) {
      console.error('Error resetting parking lot:', err);
      setError(err.response?.data?.error || err.message || 'Failed to reset parking lot');
      toast.error('Failed to reset parking lot');
    } finally {
      setSaving(false);
    }
  };

  // Employee management functions
  const fetchEmployees = async () => {
    setLoadingEmployees(true);
    setError(null);

    try {
      const response = await axios.get('/api/users');
      setEmployees(response.data);
    } catch (err) {
      console.error('Error fetching employees:', err);
      setError(err.response?.data?.error || 'Failed to load employees');
      toast.error('Failed to load employees');
    } finally {
      setLoadingEmployees(false);
    }
  };

  const handleEmployeeFormChange = (e) => {
    const { name, value } = e.target;
    setEmployeeForm({
      ...employeeForm,
      [name]: value
    });
  };

  const openEmployeeModal = (employee = null) => {
    if (employee) {
      // Edit mode
      setCurrentEmployee(employee);
      setEmployeeForm({
        username: employee.username,
        email: employee.email,
        password: '', // Don't include password when editing
        firstName: employee.firstName || '',
        lastName: employee.lastName || '',
        phoneNumber: employee.phoneNumber || '',
        role: employee.role
      });
    } else {
      // Create mode
      setCurrentEmployee(null);
      setEmployeeForm({
        username: '',
        email: '',
        password: '',
        firstName: '',
        lastName: '',
        phoneNumber: '',
        role: 'STAFF'
      });
    }
    setShowEmployeeModal(true);
  };

  const openDeleteModal = (employee) => {
    setCurrentEmployee(employee);
    setShowDeleteModal(true);
  };

  const openResetPasswordModal = (employee) => {
    setCurrentEmployee(employee);
    setNewPassword('');
    setShowResetPasswordModal(true);
  };

  const handleCreateEmployee = async () => {
    setSaving(true);
    setError(null);

    try {
      await axios.post('/api/users', employeeForm);
      toast.success('Employee created successfully');
      setShowEmployeeModal(false);
      fetchEmployees();
    } catch (err) {
      console.error('Error creating employee:', err);
      setError(err.response?.data?.error || 'Failed to create employee');
      toast.error('Failed to create employee');
    } finally {
      setSaving(false);
    }
  };

  const handleUpdateEmployee = async () => {
    setSaving(true);
    setError(null);

    try {
      // Remove password from the form if it's empty
      const updateData = { ...employeeForm };
      if (!updateData.password) {
        delete updateData.password;
      }

      await axios.put(`/api/users/${currentEmployee.id}`, updateData);
      toast.success('Employee updated successfully');
      setShowEmployeeModal(false);
      fetchEmployees();
    } catch (err) {
      console.error('Error updating employee:', err);
      setError(err.response?.data?.error || 'Failed to update employee');
      toast.error('Failed to update employee');
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteEmployee = async () => {
    setSaving(true);
    setError(null);

    try {
      await axios.delete(`/api/users/${currentEmployee.id}`);
      toast.success('Employee deleted successfully');
      setShowDeleteModal(false);
      fetchEmployees();
    } catch (err) {
      console.error('Error deleting employee:', err);
      setError(err.response?.data?.error || 'Failed to delete employee');
      toast.error('Failed to delete employee');
    } finally {
      setSaving(false);
    }
  };

  const handleResetPassword = async () => {
    setSaving(true);
    setError(null);

    try {
      const response = await axios.post(`/api/users/${currentEmployee.id}/reset-password`);
      setNewPassword(response.data.newPassword);
      toast.success('Password reset successfully');
    } catch (err) {
      console.error('Error resetting password:', err);
      setError(err.response?.data?.error || 'Failed to reset password');
      toast.error('Failed to reset password');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <Container className="mt-4 text-center">
        <Spinner animation="border" role="status">
          <span className="visually-hidden">Loading...</span>
        </Spinner>
        <p className="mt-2">Loading settings...</p>
      </Container>
    );
  }

  return (
    <Container fluid className="py-4">
      <h2 className="mb-4">
        <FaCog className="me-2" />
        System Settings
      </h2>
      <p className="text-muted">Configure system settings such as parking fees and discounts</p>

      {error && <Alert variant="danger">{error}</Alert>}
      {success && <Alert variant="success">{success}</Alert>}

      <Tabs
        activeKey={activeTab}
        onSelect={(k) => setActiveTab(k)}
        className="mb-4"
      >
        <Tab eventKey="employees" title={<span><FaUsers className="me-2" />Employee Management</span>}>
          <Card className="shadow-sm">
            <Card.Header className="bg-primary text-white d-flex justify-content-between align-items-center">
              <h5 className="mb-0">Employee Management</h5>
              <Button variant="light" size="sm" onClick={() => openEmployeeModal()}>
                <FaUserPlus className="me-1" /> Add Employee
              </Button>
            </Card.Header>
            <Card.Body>
              {loadingEmployees ? (
                <div className="text-center py-4">
                  <Spinner animation="border" role="status">
                    <span className="visually-hidden">Loading employees...</span>
                  </Spinner>
                  <p className="mt-2">Loading employees...</p>
                </div>
              ) : (
                <>
                  {employees.length === 0 ? (
                    <Alert variant="info">
                      No employees found. Click "Add Employee" to create one.
                    </Alert>
                  ) : (
                    <Table striped bordered hover responsive>
                      <thead>
                        <tr>
                          <th>Employee ID</th>
                          <th>Username</th>
                          <th>Name</th>
                          <th>Email</th>
                          <th>Role</th>
                          <th>Status</th>
                          <th>Actions</th>
                        </tr>
                      </thead>
                      <tbody>
                        {employees.map((employee) => (
                          <tr key={employee.id}>
                            <td>{employee.employeeId}</td>
                            <td>{employee.username}</td>
                            <td>{employee.firstName} {employee.lastName}</td>
                            <td>{employee.email}</td>
                            <td>
                              <Badge bg={employee.role === 'ADMIN' ? 'primary' : 'secondary'}>
                                {employee.role}
                              </Badge>
                            </td>
                            <td>
                              <Badge bg={employee.isActive ? 'success' : 'danger'}>
                                {employee.isActive ? 'Active' : 'Inactive'}
                              </Badge>
                            </td>
                            <td>
                              <Button
                                variant="outline-primary"
                                size="sm"
                                className="me-1"
                                onClick={() => openEmployeeModal(employee)}
                                title="Edit"
                              >
                                <FaUserEdit />
                              </Button>
                              <Button
                                variant="outline-warning"
                                size="sm"
                                className="me-1"
                                onClick={() => openResetPasswordModal(employee)}
                                title="Reset Password"
                              >
                                <FaKey />
                              </Button>
                              <Button
                                variant="outline-danger"
                                size="sm"
                                onClick={() => openDeleteModal(employee)}
                                title="Delete"
                              >
                                <FaTrash />
                              </Button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </Table>
                  )}
                </>
              )}
            </Card.Body>
          </Card>
        </Tab>

        <Tab eventKey="parking-fees" title={<span><FaMoneyBillWave className="me-2" />Parking Fees</span>}>
          <Card className="shadow-sm">
            <Card.Header className="bg-primary text-white">
              <h5 className="mb-0">Parking Fee Settings</h5>
            </Card.Header>
            <Card.Body>
              <Form>
                <Row>
                  <Col md={6}>
                    <h6 className="mb-3">Walk-in (Casual) Parking Fees</h6>
                    <Form.Group className="mb-3">
                      <Form.Label>Motorbike Fee (VND)</Form.Label>
                      <Form.Control
                        type="number"
                        name="casualMotorbikeFee"
                        value={parkingFees.casualMotorbikeFee}
                        onChange={handleParkingFeeChange}
                        min="0"
                      />
                      <Form.Text className="text-muted">
                        Fee for casual motorbike parking per session
                      </Form.Text>
                    </Form.Group>

                    <Form.Group className="mb-3">
                      <Form.Label>Car Fee (VND)</Form.Label>
                      <Form.Control
                        type="number"
                        name="casualCarFee"
                        value={parkingFees.casualCarFee}
                        onChange={handleParkingFeeChange}
                        min="0"
                      />
                      <Form.Text className="text-muted">
                        Fee for casual car parking per session
                      </Form.Text>
                    </Form.Group>
                  </Col>

                  <Col md={6}>
                    <h6 className="mb-3">Monthly Parking Fees (Base Rate)</h6>
                    <Form.Group className="mb-3">
                      <Form.Label>Monthly Motorbike Fee (VND)</Form.Label>
                      <Form.Control
                        type="number"
                        name="monthlyMotorbikeFee"
                        value={parkingFees.monthlyMotorbikeFee}
                        onChange={handleParkingFeeChange}
                        min="0"
                      />
                      <Form.Text className="text-muted">
                        Base fee for monthly motorbike parking (per month)
                      </Form.Text>
                    </Form.Group>

                    <Form.Group className="mb-3">
                      <Form.Label>Monthly Car Fee (VND)</Form.Label>
                      <Form.Control
                        type="number"
                        name="monthlyCarFee"
                        value={parkingFees.monthlyCarFee}
                        onChange={handleParkingFeeChange}
                        min="0"
                      />
                      <Form.Text className="text-muted">
                        Base fee for monthly car parking (per month)
                      </Form.Text>
                    </Form.Group>
                  </Col>
                </Row>

                <div className="d-flex justify-content-end mt-3">
                  <Button
                    variant="outline-secondary"
                    className="me-2"
                    onClick={resetParkingFees}
                    disabled={saving}
                  >
                    <FaUndo className="me-2" />
                    Reset
                  </Button>
                  <Button
                    variant="primary"
                    onClick={saveParkingFees}
                    disabled={saving}
                  >
                    {saving ? (
                      <>
                        <Spinner
                          as="span"
                          animation="border"
                          size="sm"
                          role="status"
                          aria-hidden="true"
                        />
                        <span className="ms-2">Saving...</span>
                      </>
                    ) : (
                      <>
                        <FaSave className="me-2" />
                        Save Changes
                      </>
                    )}
                  </Button>
                </div>
              </Form>
            </Card.Body>
          </Card>
        </Tab>

        <Tab eventKey="discounts" title={<span><FaPercentage className="me-2" />Discounts</span>}>
          <Card className="shadow-sm">
            <Card.Header className="bg-primary text-white">
              <h5 className="mb-0">Discount Settings</h5>
            </Card.Header>
            <Card.Body>
              <p className="text-muted mb-4">
                Configure discount tiers for monthly parking subscriptions based on duration.
              </p>

              <Table striped bordered hover responsive>
                <thead>
                  <tr>
                    <th>Min Months</th>
                    <th>Max Months</th>
                    <th>Discount (%)</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {discountSettings.discountTiers.map((tier, index) => (
                    <tr key={index}>
                      <td>
                        <Form.Control
                          type="number"
                          value={tier.minMonths}
                          onChange={(e) => handleDiscountTierChange(index, 'minMonths', e.target.value)}
                          min="1"
                        />
                      </td>
                      <td>
                        <Form.Control
                          type="number"
                          value={tier.maxMonths === 2147483647 ? 999 : tier.maxMonths}
                          onChange={(e) => handleDiscountTierChange(index, 'maxMonths', e.target.value === '999' ? 2147483647 : e.target.value)}
                          min={tier.minMonths}
                        />
                        {tier.maxMonths === 2147483647 && (
                          <Form.Text className="text-muted">
                            (Unlimited)
                          </Form.Text>
                        )}
                      </td>
                      <td>
                        <Form.Control
                          type="number"
                          value={tier.discountPercentage}
                          onChange={(e) => handleDiscountTierChange(index, 'discountPercentage', e.target.value)}
                          min="0"
                          max="100"
                        />
                      </td>
                      <td>
                        <Button
                          variant="danger"
                          size="sm"
                          onClick={() => removeDiscountTier(index)}
                          disabled={discountSettings.discountTiers.length <= 1}
                        >
                          Remove
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </Table>

              <Button
                variant="outline-primary"
                onClick={addDiscountTier}
                className="mb-3"
              >
                Add Discount Tier
              </Button>

              <div className="d-flex justify-content-end mt-3">
                <Button
                  variant="outline-secondary"
                  className="me-2"
                  onClick={resetDiscountSettings}
                  disabled={saving}
                >
                  <FaUndo className="me-2" />
                  Reset
                </Button>
                <Button
                  variant="primary"
                  onClick={saveDiscountSettings}
                  disabled={saving}
                >
                  {saving ? (
                    <>
                      <Spinner
                        as="span"
                        animation="border"
                        size="sm"
                        role="status"
                        aria-hidden="true"
                      />
                      <span className="ms-2">Saving...</span>
                    </>
                  ) : (
                    <>
                      <FaSave className="me-2" />
                      Save Changes
                    </>
                  )}
                </Button>
              </div>
            </Card.Body>
          </Card>
        </Tab>

        <Tab eventKey="parking-spaces" title={<span><FaParking className="me-2" />Parking Spaces</span>}>
          <Card className="shadow-sm">
            <Card.Header className="bg-primary text-white">
              <h5 className="mb-0">Parking Space Settings</h5>
            </Card.Header>
            <Card.Body>
              <p className="text-muted mb-4">
                Configure the number of parking spaces for each vehicle type.
              </p>

              <Row className="mb-4">
                <Col md={6}>
                  <Card className="h-100">
                    <Card.Header className="bg-light">
                      <h6 className="mb-0">Current Configuration</h6>
                    </Card.Header>
                    <Card.Body>
                      <Table bordered>
                        <thead>
                          <tr>
                            <th>Vehicle Type</th>
                            <th>Configured</th>
                            <th>Actual</th>
                            <th>Occupied</th>
                          </tr>
                        </thead>
                        <tbody>
                          <tr>
                            <td>Motorbikes</td>
                            <td>{parkingConfiguration.configuredMotorcycleSlots}</td>
                            <td>{parkingConfiguration.actualMotorcycleSlots}</td>
                            <td>{parkingConfiguration.occupiedMotorcycleSlots}</td>
                          </tr>
                          <tr>
                            <td>Cars</td>
                            <td>{parkingConfiguration.configuredCarSlots}</td>
                            <td>{parkingConfiguration.actualCarSlots}</td>
                            <td>{parkingConfiguration.occupiedCarSlots}</td>
                          </tr>
                        </tbody>
                      </Table>
                      <Button
                        variant="outline-secondary"
                        size="sm"
                        className="mt-2"
                        onClick={fetchParkingConfiguration}
                      >
                        <FaSync className="me-2" />
                        Refresh
                      </Button>
                    </Card.Body>
                  </Card>
                </Col>

                <Col md={6}>
                  <Card className="h-100">
                    <Card.Header className="bg-light">
                      <h6 className="mb-0">Adjust Parking Spaces</h6>
                    </Card.Header>
                    <Card.Body>
                      <Form>
                        <Form.Group className="mb-3">
                          <Form.Label>Number of Motorbike Spaces</Form.Label>
                          <Form.Control
                            type="number"
                            name="motorcycleSlots"
                            value={parkingSpaces.motorcycleSlots}
                            onChange={handleParkingSpaceChange}
                            min="0"
                          />
                          <Form.Text className="text-muted">
                            Total number of parking spaces for motorbikes
                          </Form.Text>
                        </Form.Group>

                        <Form.Group className="mb-3">
                          <Form.Label>Number of Car Spaces</Form.Label>
                          <Form.Control
                            type="number"
                            name="carSlots"
                            value={parkingSpaces.carSlots}
                            onChange={handleParkingSpaceChange}
                            min="0"
                          />
                          <Form.Text className="text-muted">
                            Total number of parking spaces for cars
                          </Form.Text>
                        </Form.Group>

                        <Alert variant="info">
                          <p className="mb-0">
                            <strong>Note:</strong> Increasing the number of spaces will add new slots immediately.
                            Reducing the number of spaces will only work if there are no vehicles parked in the slots to be removed.
                          </p>
                        </Alert>
                      </Form>
                    </Card.Body>
                  </Card>
                </Col>
              </Row>

              <div className="d-flex justify-content-between mt-3">
                <Button
                  variant="danger"
                  onClick={() => setShowResetModal(true)}
                  disabled={saving}
                >
                  <FaExclamationTriangle className="me-2" />
                  Reset Entire Parking Lot
                </Button>

                <div>
                  <Button
                    variant="outline-secondary"
                    className="me-2"
                    onClick={resetParkingSpaces}
                    disabled={saving}
                  >
                    <FaUndo className="me-2" />
                    Reset
                  </Button>
                  <Button
                    variant="primary"
                    onClick={saveParkingSpaces}
                    disabled={saving}
                  >
                    {saving ? (
                      <>
                        <Spinner
                          as="span"
                          animation="border"
                          size="sm"
                          role="status"
                          aria-hidden="true"
                        />
                        <span className="ms-2">Saving...</span>
                      </>
                    ) : (
                      <>
                        <FaSave className="me-2" />
                        Save Changes
                      </>
                    )}
                  </Button>
                </div>
              </div>
            </Card.Body>
          </Card>
        </Tab>
      </Tabs>

      {/* Reset Confirmation Modal */}
      <Modal show={showResetModal} onHide={() => setShowResetModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>
            <FaExclamationTriangle className="text-danger me-2" />
            Reset Parking Lot
          </Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <p>
            <strong>Warning:</strong> This will delete all parking slots and recreate them according to the current settings.
          </p>
          <p>
            This operation can only be performed when there are no vehicles currently parked in the parking lot.
          </p>
          <p className="mb-0">
            Are you sure you want to proceed?
          </p>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowResetModal(false)}>
            Cancel
          </Button>
          <Button variant="danger" onClick={handleResetParkingLot} disabled={saving}>
            {saving ? (
              <>
                <Spinner
                  as="span"
                  animation="border"
                  size="sm"
                  role="status"
                  aria-hidden="true"
                />
                <span className="ms-2">Processing...</span>
              </>
            ) : (
              'Reset Parking Lot'
            )}
          </Button>
        </Modal.Footer>
      </Modal>

      {/* Employee Modal (Create/Edit) */}
      <Modal show={showEmployeeModal} onHide={() => setShowEmployeeModal(false)} size="lg">
        <Modal.Header closeButton>
          <Modal.Title>
            {currentEmployee ? (
              <>
                <FaUserEdit className="me-2" />
                Edit Employee
              </>
            ) : (
              <>
                <FaUserPlus className="me-2" />
                Add New Employee
              </>
            )}
          </Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {error && <Alert variant="danger">{error}</Alert>}

          <Form>
            <Row>
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>Username</Form.Label>
                  <Form.Control
                    type="text"
                    name="username"
                    value={employeeForm.username}
                    onChange={handleEmployeeFormChange}
                    required
                  />
                </Form.Group>
              </Col>
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>Email</Form.Label>
                  <Form.Control
                    type="email"
                    name="email"
                    value={employeeForm.email}
                    onChange={handleEmployeeFormChange}
                    required
                  />
                </Form.Group>
              </Col>
            </Row>

            <Row>
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>First Name</Form.Label>
                  <Form.Control
                    type="text"
                    name="firstName"
                    value={employeeForm.firstName}
                    onChange={handleEmployeeFormChange}
                  />
                </Form.Group>
              </Col>
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>Last Name</Form.Label>
                  <Form.Control
                    type="text"
                    name="lastName"
                    value={employeeForm.lastName}
                    onChange={handleEmployeeFormChange}
                  />
                </Form.Group>
              </Col>
            </Row>

            <Row>
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>Phone Number</Form.Label>
                  <Form.Control
                    type="text"
                    name="phoneNumber"
                    value={employeeForm.phoneNumber}
                    onChange={handleEmployeeFormChange}
                  />
                </Form.Group>
              </Col>
              <Col md={6}>
                <Form.Group className="mb-3">
                  <Form.Label>Role</Form.Label>
                  <Form.Select
                    name="role"
                    value={employeeForm.role}
                    onChange={handleEmployeeFormChange}
                    required
                  >
                    <option value="ADMIN">Administrator</option>
                    <option value="STAFF">Staff</option>
                  </Form.Select>
                </Form.Group>
              </Col>
            </Row>

            {currentEmployee && (
              <Row>
                <Col md={6}>
                  <Form.Group className="mb-3">
                    <Form.Label>Status</Form.Label>
                    <Form.Select
                      name="isActive"
                      value={employeeForm.isActive}
                      onChange={handleEmployeeFormChange}
                    >
                      <option value={true}>Active</option>
                      <option value={false}>Inactive</option>
                    </Form.Select>
                  </Form.Group>
                </Col>
              </Row>
            )}

            {!currentEmployee && (
              <Row>
                <Col md={12}>
                  <Form.Group className="mb-3">
                    <Form.Label>Password</Form.Label>
                    <Form.Control
                      type="password"
                      name="password"
                      value={employeeForm.password}
                      onChange={handleEmployeeFormChange}
                      required={!currentEmployee}
                    />
                    {!currentEmployee && (
                      <Form.Text className="text-muted">
                        Password must be at least 6 characters long.
                      </Form.Text>
                    )}
                  </Form.Group>
                </Col>
              </Row>
            )}
          </Form>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowEmployeeModal(false)}>
            Cancel
          </Button>
          <Button
            variant="primary"
            onClick={currentEmployee ? handleUpdateEmployee : handleCreateEmployee}
            disabled={saving}
          >
            {saving ? (
              <>
                <Spinner
                  as="span"
                  animation="border"
                  size="sm"
                  role="status"
                  aria-hidden="true"
                  className="me-2"
                />
                {currentEmployee ? 'Updating...' : 'Creating...'}
              </>
            ) : (
              currentEmployee ? 'Update Employee' : 'Create Employee'
            )}
          </Button>
        </Modal.Footer>
      </Modal>

      {/* Delete Employee Confirmation Modal */}
      <Modal show={showDeleteModal} onHide={() => setShowDeleteModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title className="text-danger">
            <FaTrash className="me-2" />
            Delete Employee
          </Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {currentEmployee && (
            <>
              <p>Are you sure you want to delete the following employee?</p>
              <ul>
                <li><strong>Employee ID:</strong> {currentEmployee.employeeId}</li>
                <li><strong>Username:</strong> {currentEmployee.username}</li>
                <li><strong>Name:</strong> {currentEmployee.firstName} {currentEmployee.lastName}</li>
                <li><strong>Email:</strong> {currentEmployee.email}</li>
              </ul>
              <p className="text-danger fw-bold">This action cannot be undone!</p>
            </>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowDeleteModal(false)}>
            Cancel
          </Button>
          <Button
            variant="danger"
            onClick={handleDeleteEmployee}
            disabled={saving}
          >
            {saving ? (
              <>
                <Spinner
                  as="span"
                  animation="border"
                  size="sm"
                  role="status"
                  aria-hidden="true"
                  className="me-2"
                />
                Deleting...
              </>
            ) : (
              'Delete Employee'
            )}
          </Button>
        </Modal.Footer>
      </Modal>

      {/* Reset Password Modal */}
      <Modal show={showResetPasswordModal} onHide={() => setShowResetPasswordModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>
            <FaKey className="me-2" />
            Reset Password
          </Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {currentEmployee && (
            <>
              <p>Reset password for employee:</p>
              <ul>
                <li><strong>Employee ID:</strong> {currentEmployee.employeeId}</li>
                <li><strong>Username:</strong> {currentEmployee.username}</li>
                <li><strong>Name:</strong> {currentEmployee.firstName} {currentEmployee.lastName}</li>
              </ul>

              {newPassword ? (
                <Alert variant="success">
                  <p><strong>Password has been reset!</strong></p>
                  <p>New password: <code>{newPassword}</code></p>
                  <p className="mb-0">Please provide this password to the employee.</p>
                </Alert>
              ) : (
                <p>This will generate a new random password for the employee.</p>
              )}
            </>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowResetPasswordModal(false)}>
            {newPassword ? 'Close' : 'Cancel'}
          </Button>
          {!newPassword && (
            <Button
              variant="warning"
              onClick={handleResetPassword}
              disabled={saving}
            >
              {saving ? (
                <>
                  <Spinner
                    as="span"
                    animation="border"
                    size="sm"
                    role="status"
                    aria-hidden="true"
                    className="me-2"
                  />
                  Resetting...
                </>
              ) : (
                'Reset Password'
              )}
            </Button>
          )}
        </Modal.Footer>
      </Modal>
    </Container>
  );
};

export default Settings;
