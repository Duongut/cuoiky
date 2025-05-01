import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { Tabs, Tab, Container, Alert, Spinner, Form, Button, Table, Card, Badge, Row, Col, InputGroup, Dropdown, Modal } from 'react-bootstrap';
import { FaSearch, FaFilter, FaSync, FaCamera, FaCarSide, FaMotorcycle, FaHistory, FaFileExcel, FaFilePdf, FaFileCsv, FaDownload, FaPrint, FaCarAlt, FaEdit, FaEye } from 'react-icons/fa';
import axios from 'axios';
import CheckIn from './CheckIn';
import CheckOut from './CheckOut';
import CameraMonitoring from './CameraMonitoring';
import DatePicker from 'react-datepicker';
import 'react-datepicker/dist/react-datepicker.css';
import { toast } from 'react-toastify';
import { debounce } from 'lodash';

const VEHICLE_TYPE_MAP = {
  'CAR': 'Xe ô tô',
  'MOTORBIKE': 'Xe máy',
  'ALL': 'Tất cả'
};

const STATUS_MAP = {
  'PARKED': 'Đang đỗ',
  'OCCUPIED': 'Đang đỗ',
  'EXITED': 'Đã rời đi',
  'ALL': 'Tất cả'
};

const AccessControl = () => {
  const [activeTab, setActiveTab] = useState('manual-checkin');
  const [vehicles, setVehicles] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [vehicleDetailLoading, setVehicleDetailLoading] = useState(false);

  const [licensePlate, setLicensePlate] = useState('');
  const [vehicleType, setVehicleType] = useState('ALL');
  const [status, setStatus] = useState('ALL');
  const [parkingLocation, setParkingLocation] = useState('');
  const [startDate, setStartDate] = useState(new Date(new Date().setDate(new Date().getDate() - 7)));
  const [endDate, setEndDate] = useState(new Date());
  const [sortBy, setSortBy] = useState('entryTime');
  const [sortOrder, setSortOrder] = useState('desc');

  const [showVehicleModal, setShowVehicleModal] = useState(false);
  const [selectedVehicle, setSelectedVehicle] = useState(null);
  const [vehicleHistory, setVehicleHistory] = useState([]);
  const [weeklyActivity, setWeeklyActivity] = useState(null);

  const [isSearching, setIsSearching] = useState(false);

  const [currentPage, setCurrentPage] = useState(1);
  const [itemsPerPage, setItemsPerPage] = useState(10);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  useEffect(() => {
    if (activeTab === 'vehicle-history') {
      fetchVehicleHistory();
    }
  }, [activeTab]);

  useEffect(() => {
    if (activeTab === 'vehicle-history') {
      const intervalId = setInterval(() => {
        fetchVehicleHistory(true);
      }, 60000);

      return () => clearInterval(intervalId);
    }
  }, [activeTab]);

  const debouncedSearch = useCallback(
    debounce(() => {
      fetchVehicleHistory();
      setIsSearching(false);
    }, 500),
    [licensePlate, vehicleType, status, parkingLocation, startDate, endDate, sortBy, sortOrder, currentPage]
  );

  useEffect(() => {
    if (activeTab === 'vehicle-history' && !loading) {
      setIsSearching(true);
      debouncedSearch();
    }
  }, [licensePlate, vehicleType, status, parkingLocation, sortBy, sortOrder, currentPage]);

  const fetchVehicleHistory = async (silentRefresh = false) => {
    try {
      if (!silentRefresh) setLoading(true);
      setError(null);

      const params = new URLSearchParams();
      params.append('startDate', startDate.toISOString());
      params.append('endDate', endDate.toISOString());
      params.append('page', currentPage);
      params.append('pageSize', itemsPerPage);
      params.append('sortBy', sortBy);
      params.append('sortOrder', sortOrder);

      if (licensePlate) params.append('licensePlate', licensePlate);
      if (vehicleType !== 'ALL') params.append('vehicleType', vehicleType);
      if (status !== 'ALL') params.append('status', status);
      if (parkingLocation) params.append('slotId', parkingLocation);

      const response = await axios.get(`/api/vehicle/history?${params.toString()}`);

      if (response.data && response.data.items) {
        setVehicles(response.data.items);
        setTotalItems(response.data.totalCount || response.data.items.length);
        setTotalPages(response.data.totalPages || Math.ceil(response.data.items.length / itemsPerPage));
      } else {
        setVehicles(response.data || []);
        setTotalItems(response.data.length || 0);
        setTotalPages(Math.ceil((response.data.length || 0) / itemsPerPage));
      }
    } catch (error) {
      console.error('Error fetching vehicle history:', error);
      if (!silentRefresh) {
        setError('Không thể tải lịch sử ra vào. Vui lòng thử lại.');
        toast.error('Không thể tải lịch sử ra vào');
      }
    } finally {
      if (!silentRefresh) setLoading(false);
    }
  };

  const handleSearch = () => {
    setCurrentPage(1);
    fetchVehicleHistory();
  };

  const handleReset = () => {
    setLicensePlate('');
    setVehicleType('ALL');
    setStatus('ALL');
    setParkingLocation('');
    setStartDate(new Date(new Date().setDate(new Date().getDate() - 7)));
    setEndDate(new Date());
    setSortBy('entryTime');
    setSortOrder('desc');
    setCurrentPage(1);

    fetchVehicleHistory();
  };

  const handlePageChange = (page) => {
    setCurrentPage(page);
  };

  const handleSort = (field) => {
    if (sortBy === field) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
    } else {
      setSortBy(field);
      setSortOrder('asc');
    }
  };

  const handleExport = async (format) => {
    try {
      const params = new URLSearchParams();
      params.append('startDate', startDate.toISOString());
      params.append('endDate', endDate.toISOString());

      if (licensePlate) params.append('licensePlate', licensePlate);
      if (vehicleType !== 'ALL') params.append('vehicleType', vehicleType);
      if (status !== 'ALL') params.append('status', status);
      if (parkingLocation) params.append('slotId', parkingLocation);

      const url = `/api/vehicle/export/${format}?${params.toString()}`;

      window.open(url, '_blank');
      toast.success(`Xuất báo cáo định dạng ${format.toUpperCase()} thành công`);
    } catch (err) {
      console.error(`Lỗi khi xuất ${format}:`, err);
      toast.error(`Xuất báo cáo thất bại: ${err.message}`);
    }
  };

  const handlePrint = () => {
    window.print();
  };

  const openVehicleDetail = async (vehicle) => {
    setSelectedVehicle(vehicle);
    setShowVehicleModal(true);

    try {
      setVehicleDetailLoading(true);

      const [historyResponse, activityResponse] = await Promise.all([
        axios.get(`/api/vehicle/${vehicle.vehicleId}/history`),
        axios.get(`/api/vehicle/${vehicle.vehicleId}/activity`)
      ]);

      setVehicleHistory(historyResponse.data || []);
      setWeeklyActivity(activityResponse.data || null);

      setVehicleDetailLoading(false);
    } catch (error) {
      console.error('Error fetching vehicle details:', error);
      toast.error('Không thể tải chi tiết phương tiện');
      setVehicleDetailLoading(false);
    }
  };

  const formatDate = (dateString) => {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleString('vi-VN');
  };

  const getStatusBadge = (status) => {
    if (status === 'PARKED' || status === 'OCCUPIED') {
      return <Badge bg="success">Đang đỗ xe</Badge>;
    } else if (status === 'EXITED') {
      return <Badge bg="secondary">Đã rời đi</Badge>;
    } else {
      return <Badge bg="info">{status}</Badge>;
    }
  };

  const getVehicleTypeBadge = (type) => {
    if (type === 'CAR') {
      return <Badge bg="primary"><FaCarAlt className="me-1" /> Xe ô tô</Badge>;
    } else if (type === 'MOTORBIKE') {
      return <Badge bg="info"><FaMotorcycle className="me-1" /> Xe máy</Badge>;
    } else {
      return <Badge bg="secondary">{type}</Badge>;
    }
  };

  const calculateDuration = (entryTime, exitTime) => {
    if (!entryTime) return 'N/A';

    const start = new Date(entryTime);
    const end = exitTime ? new Date(exitTime) : new Date();

    const diffMs = end - start;
    const diffHrs = Math.floor(diffMs / (1000 * 60 * 60));
    const diffMins = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));

    if (diffHrs > 0) {
      return `${diffHrs} giờ ${diffMins} phút`;
    } else {
      return `${diffMins} phút`;
    }
  };

  const renderPagination = () => {
    const pages = [];

    const startPage = Math.max(1, currentPage - 2);
    const endPage = Math.min(totalPages, startPage + 4);

    pages.push(
      <Button
        key="prev"
        variant="outline-secondary"
        size="sm"
        className="me-1"
        disabled={currentPage === 1}
        onClick={() => handlePageChange(currentPage - 1)}
      >
        &laquo;
      </Button>
    );

    if (startPage > 1) {
      pages.push(
        <Button
          key="1"
          variant={currentPage === 1 ? "primary" : "outline-secondary"}
          size="sm"
          className="me-1"
          onClick={() => handlePageChange(1)}
        >
          1
        </Button>
      );

      if (startPage > 2) {
        pages.push(<span key="ellipsis1" className="mx-1">...</span>);
      }
    }

    for (let i = startPage; i <= endPage; i++) {
      pages.push(
        <Button
          key={i}
          variant={currentPage === i ? "primary" : "outline-secondary"}
          size="sm"
          className="me-1"
          onClick={() => handlePageChange(i)}
        >
          {i}
        </Button>
      );
    }

    if (endPage < totalPages - 1) {
      pages.push(<span key="ellipsis2" className="mx-1">...</span>);
    }

    if (endPage < totalPages) {
      pages.push(
        <Button
          key={totalPages}
          variant={currentPage === totalPages ? "primary" : "outline-secondary"}
          size="sm"
          className="me-1"
          onClick={() => handlePageChange(totalPages)}
        >
          {totalPages}
        </Button>
      );
    }

    pages.push(
      <Button
        key="next"
        variant="outline-secondary"
        size="sm"
        disabled={currentPage === totalPages}
        onClick={() => handlePageChange(currentPage + 1)}
      >
        &raquo;
      </Button>
    );

    return (
      <div className="d-flex justify-content-center mt-3">
        {pages}
      </div>
    );
  };

  const renderVehicleHistory = () => {
    return (
      <div>
        <div className="d-flex justify-content-between align-items-center mb-4">
          <h2>Lịch sử ra vào</h2>
          <div>
            <Button variant="outline-primary" onClick={handlePrint} className="me-2">
              <FaPrint className="me-1" /> In báo cáo
            </Button>
            <Dropdown as={Button.Group}>
              <Dropdown.Toggle variant="outline-success" id="dropdown-export">
                <FaDownload className="me-1" /> Xuất báo cáo
              </Dropdown.Toggle>
              <Dropdown.Menu>
                <Dropdown.Item onClick={() => handleExport('excel')}>
                  <FaFileExcel className="me-2" /> Excel
                </Dropdown.Item>
                <Dropdown.Item onClick={() => handleExport('pdf')}>
                  <FaFilePdf className="me-2" /> PDF
                </Dropdown.Item>
                <Dropdown.Item onClick={() => handleExport('csv')}>
                  <FaFileCsv className="me-2" /> CSV
                </Dropdown.Item>
              </Dropdown.Menu>
            </Dropdown>
          </div>
        </div>

        <Card className="mb-4">
          <Card.Header>
            <FaFilter className="me-2" />
            Bộ lọc & tìm kiếm
          </Card.Header>
          <Card.Body>
            <Row>
              <Col md={3}>
                <Form.Group className="mb-3">
                  <Form.Label>Từ ngày</Form.Label>
                  <DatePicker
                    selected={startDate}
                    onChange={date => setStartDate(date)}
                    selectsStart
                    startDate={startDate}
                    endDate={endDate}
                    className="form-control"
                    dateFormat="dd/MM/yyyy"
                  />
                </Form.Group>
              </Col>
              <Col md={3}>
                <Form.Group className="mb-3">
                  <Form.Label>Đến ngày</Form.Label>
                  <DatePicker
                    selected={endDate}
                    onChange={date => setEndDate(date)}
                    selectsEnd
                    startDate={startDate}
                    endDate={endDate}
                    minDate={startDate}
                    className="form-control"
                    dateFormat="dd/MM/yyyy"
                  />
                </Form.Group>
              </Col>
              <Col md={3}>
                <Form.Group className="mb-3">
                  <Form.Label>Biển số xe</Form.Label>
                  <Form.Control
                    type="text"
                    placeholder="Nhập biển số"
                    value={licensePlate}
                    onChange={e => setLicensePlate(e.target.value)}
                  />
                </Form.Group>
              </Col>
              <Col md={3}>
                <Form.Group className="mb-3">
                  <Form.Label>Loại xe</Form.Label>
                  <Form.Select
                    value={vehicleType}
                    onChange={e => setVehicleType(e.target.value)}
                  >
                    <option value="ALL">Tất cả loại xe</option>
                    <option value="CAR">Xe ô tô</option>
                    <option value="MOTORBIKE">Xe máy</option>
                  </Form.Select>
                </Form.Group>
              </Col>
            </Row>
            <Row>
              <Col md={3}>
                <Form.Group className="mb-3">
                  <Form.Label>Trạng thái</Form.Label>
                  <Form.Select
                    value={status}
                    onChange={e => setStatus(e.target.value)}
                  >
                    <option value="ALL">Tất cả trạng thái</option>
                    <option value="PARKED">Đang đỗ xe</option>
                    <option value="EXITED">Đã rời đi</option>
                  </Form.Select>
                </Form.Group>
              </Col>
              <Col md={3}>
                <Form.Group className="mb-3">
                  <Form.Label>Vị trí đỗ xe</Form.Label>
                  <Form.Control
                    type="text"
                    placeholder="Nhập mã vị trí"
                    value={parkingLocation}
                    onChange={e => setParkingLocation(e.target.value)}
                  />
                </Form.Group>
              </Col>
              <Col md={3}>
                <Form.Group className="mb-3">
                  <Form.Label>Sắp xếp theo</Form.Label>
                  <Form.Select
                    value={sortBy}
                    onChange={e => setSortBy(e.target.value)}
                  >
                    <option value="entryTime">Thời gian vào</option>
                    <option value="exitTime">Thời gian ra</option>
                    <option value="licensePlate">Biển số xe</option>
                  </Form.Select>
                </Form.Group>
              </Col>
              <Col md={3}>
                <Form.Group className="mb-3">
                  <Form.Label>Thứ tự</Form.Label>
                  <Form.Select
                    value={sortOrder}
                    onChange={e => setSortOrder(e.target.value)}
                  >
                    <option value="desc">Giảm dần</option>
                    <option value="asc">Tăng dần</option>
                  </Form.Select>
                </Form.Group>
              </Col>
            </Row>
            <Row>
              <Col md={12} className="d-flex align-items-end">
                <Button variant="primary" onClick={handleSearch} className="me-2">
                  <FaSearch className="me-2" />
                  Tìm kiếm
                </Button>
                <Button variant="outline-secondary" onClick={handleReset}>
                  <FaSync className="me-2" />
                  Đặt lại bộ lọc
                </Button>
              </Col>
            </Row>
          </Card.Body>
        </Card>

        {error && <Alert variant="danger">{error}</Alert>}

        <Card>
          <Card.Header className="d-flex justify-content-between align-items-center">
            <div>Danh sách xe ra vào</div>
            <div className="text-muted">Tìm thấy: {totalItems} kết quả</div>
          </Card.Header>
          <Card.Body>
            {loading ? (
              <div className="text-center my-4">
                <Spinner animation="border" role="status">
                  <span className="visually-hidden">Đang tải...</span>
                </Spinner>
              </div>
            ) : isSearching ? (
              <div className="text-center my-4">
                <Spinner animation="border" size="sm" role="status" />
                <span className="ms-2">Đang tìm kiếm...</span>
              </div>
            ) : vehicles.length === 0 ? (
              <Alert variant="info">Không tìm thấy dữ liệu xe cho các bộ lọc đã chọn.</Alert>
            ) : (
              <div className="table-responsive">
                <Table hover>
                  <thead>
                    <tr>
                      <th className="sortable" onClick={() => handleSort('vehicleId')}>
                        Mã xe {sortBy === 'vehicleId' && (sortOrder === 'asc' ? '↑' : '↓')}
                      </th>
                      <th className="sortable" onClick={() => handleSort('licensePlate')}>
                        Biển số {sortBy === 'licensePlate' && (sortOrder === 'asc' ? '↑' : '↓')}
                      </th>
                      <th className="sortable" onClick={() => handleSort('vehicleType')}>
                        Loại xe {sortBy === 'vehicleType' && (sortOrder === 'asc' ? '↑' : '↓')}
                      </th>
                      <th className="sortable" onClick={() => handleSort('slotId')}>
                        Vị trí đỗ {sortBy === 'slotId' && (sortOrder === 'asc' ? '↑' : '↓')}
                      </th>
                      <th className="sortable" onClick={() => handleSort('entryTime')}>
                        Thời gian vào {sortBy === 'entryTime' && (sortOrder === 'asc' ? '↑' : '↓')}
                      </th>
                      <th className="sortable" onClick={() => handleSort('exitTime')}>
                        Thời gian ra {sortBy === 'exitTime' && (sortOrder === 'asc' ? '↑' : '↓')}
                      </th>
                      <th>Thời gian đỗ</th>
                      <th className="sortable" onClick={() => handleSort('status')}>
                        Trạng thái {sortBy === 'status' && (sortOrder === 'asc' ? '↑' : '↓')}
                      </th>
                      <th>Thao tác</th>
                    </tr>
                  </thead>
                  <tbody>
                    {vehicles.map(vehicle => (
                      <tr key={vehicle.id} className={vehicle.status === 'PARKED' || vehicle.status === 'OCCUPIED' ? 'table-success' : ''}>
                        <td>{vehicle.vehicleId}</td>
                        <td>{vehicle.licensePlate}</td>
                        <td>{getVehicleTypeBadge(vehicle.vehicleType)}</td>
                        <td>{vehicle.slotId || 'N/A'}</td>
                        <td>{formatDate(vehicle.entryTime)}</td>
                        <td>{vehicle.exitTime ? formatDate(vehicle.exitTime) : 'N/A'}</td>
                        <td>{calculateDuration(vehicle.entryTime, vehicle.exitTime)}</td>
                        <td>{getStatusBadge(vehicle.status)}</td>
                        <td>
                          <Button variant="outline-primary" size="sm" onClick={() => openVehicleDetail(vehicle)}>
                            <FaEye className="me-1" /> Chi tiết
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </Table>
              </div>
            )}

            {!loading && vehicles.length > 0 && renderPagination()}

          </Card.Body>
          <Card.Footer className="d-flex justify-content-between">
            <div>
              <strong>Đang đỗ: </strong>
              {vehicles.filter(v => v.status === 'PARKED' || v.status === 'OCCUPIED').length} xe
            </div>
            <div className="d-flex">
              <div className="me-3">
                <FaCarAlt className="text-primary me-1" />
                <span>{vehicles.filter(v => v.vehicleType === 'CAR').length} xe ô tô</span>
              </div>
              <div>
                <FaMotorcycle className="text-info me-1" />
                <span>{vehicles.filter(v => v.vehicleType === 'MOTORBIKE').length} xe máy</span>
              </div>
            </div>
          </Card.Footer>
        </Card>
      </div>
    );
  };

  return (
    <Container fluid className="access-control-page">
      <h1 className="mb-4">Quản lý ra vào</h1>

      <Tabs
        activeKey={activeTab}
        onSelect={(k) => setActiveTab(k)}
        className="mb-4"
      >
        <Tab eventKey="manual-checkin" title={<span><FaCarSide className="me-2" />Đăng ký xe vào</span>}>
          <CheckIn />
        </Tab>
        <Tab eventKey="manual-checkout" title={<span><FaCarSide className="me-2" style={{transform: 'scaleX(-1)'}} />Đăng ký xe ra</span>}>
          <CheckOut />
        </Tab>
        <Tab eventKey="automatic" title={<span><FaCamera className="me-2" />Giám sát camera</span>}>
          <CameraMonitoring />
        </Tab>
        <Tab eventKey="vehicle-history" title={<span><FaHistory className="me-2" />Lịch sử ra vào</span>}>
          {renderVehicleHistory()}
        </Tab>
      </Tabs>

      <Modal
        show={showVehicleModal}
        onHide={() => setShowVehicleModal(false)}
        size="lg"
        centered
      >
        <Modal.Header closeButton>
          <Modal.Title>Chi tiết phương tiện</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          {selectedVehicle ? (
            <div>
              {vehicleDetailLoading ? (
                <div className="text-center my-4">
                  <Spinner animation="border" role="status">
                    <span className="visually-hidden">Đang tải...</span>
                  </Spinner>
                </div>
              ) : (
                <>
                  <Row>
                    <Col md={6}>
                      <Card className="mb-3">
                        <Card.Header>Thông tin cơ bản</Card.Header>
                        <Card.Body>
                          <p><strong>Biển số:</strong> {selectedVehicle.licensePlate}</p>
                          <p><strong>Loại xe:</strong> {VEHICLE_TYPE_MAP[selectedVehicle.vehicleType] || selectedVehicle.vehicleType}</p>
                          <p><strong>Trạng thái:</strong> {getStatusBadge(selectedVehicle.status)}</p>
                          <p><strong>Mã ID:</strong> {selectedVehicle.vehicleId}</p>
                          <p><strong>Vị trí đỗ:</strong> {selectedVehicle.slotId || 'N/A'}</p>
                        </Card.Body>
                      </Card>
                    </Col>
                    <Col md={6}>
                      <Card className="mb-3">
                        <Card.Header>Thông tin ra vào</Card.Header>
                        <Card.Body>
                          <p><strong>Thời gian vào:</strong> {formatDate(selectedVehicle.entryTime)}</p>
                          <p><strong>Thời gian ra:</strong> {selectedVehicle.exitTime ? formatDate(selectedVehicle.exitTime) : 'N/A'}</p>
                          <p><strong>Thời gian đỗ:</strong> {calculateDuration(selectedVehicle.entryTime, selectedVehicle.exitTime)}</p>
                          {selectedVehicle.isMonthlyRegistered && (
                            <p>
                              <Badge bg="info">Xe đăng ký tháng</Badge>
                            </p>
                          )}
                        </Card.Body>
                      </Card>
                    </Col>
                  </Row>

                  {weeklyActivity && (
                    <Card className="mb-3">
                      <Card.Header>Hoạt động trong 7 ngày qua</Card.Header>
                      <Card.Body>
                        <Row>
                          {Object.entries(weeklyActivity.dailyCount || {}).map(([date, count]) => (
                            <Col key={date} md={3} className="mb-3 text-center">
                              <Card>
                                <Card.Body>
                                  <h5>{new Date(date).toLocaleDateString('vi-VN')}</h5>
                                  <h3>{count}</h3>
                                  <p className="mb-0">lượt ra vào</p>
                                </Card.Body>
                              </Card>
                            </Col>
                          ))}
                        </Row>
                        <p className="mt-2 text-center">
                          <strong>Tổng lượt ra vào trong 7 ngày:</strong> {weeklyActivity.totalEntries || 0}
                        </p>
                      </Card.Body>
                    </Card>
                  )}

                  <Card>
                    <Card.Header>Lịch sử ra vào gần đây</Card.Header>
                    <Card.Body>
                      {vehicleHistory.length === 0 ? (
                        <Alert variant="info">Không có lịch sử ra vào gần đây.</Alert>
                      ) : (
                        <Table responsive hover size="sm">
                          <thead>
                            <tr>
                              <th>Vị trí</th>
                              <th>Thời gian vào</th>
                              <th>Thời gian ra</th>
                              <th>Thời lượng</th>
                            </tr>
                          </thead>
                          <tbody>
                            {vehicleHistory.map((item, index) => (
                              <tr key={index}>
                                <td>{item.slotId || 'N/A'}</td>
                                <td>{formatDate(item.entryTime)}</td>
                                <td>{item.exitTime ? formatDate(item.exitTime) : 'N/A'}</td>
                                <td>{calculateDuration(item.entryTime, item.exitTime)}</td>
                              </tr>
                            ))}
                          </tbody>
                        </Table>
                      )}
                    </Card.Body>
                  </Card>
                </>
              )}
            </div>
          ) : (
            <Alert variant="info">Không có dữ liệu.</Alert>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowVehicleModal(false)}>
            Đóng
          </Button>
        </Modal.Footer>
      </Modal>

      <style type="text/css">
        {`
        .access-control-page .sortable {
          cursor: pointer;
          user-select: none;
        }

        .access-control-page .sortable:hover {
          background-color: #f8f9fa;
        }

        .access-control-page .table-responsive {
          max-height: 600px;
          overflow-y: auto;
        }

        @media print {
          .nav-tabs, .btn, .dropdown, .form-control, .input-group,
          .card-header button, .pagination, .modal, .tab-pane:not(.active) {
            display: none !important;
          }

          .card {
            box-shadow: none !important;
            border: 1px solid #ddd !important;
          }

          .container, .container-fluid {
            width: 100% !important;
            padding: 0 !important;
            margin: 0 !important;
          }

          h1 {
            font-size: 24pt !important;
            text-align: center !important;
            margin: 20pt 0 !important;
          }

          .card-body {
            padding: 10pt !important;
          }
        }
        `}
      </style>
    </Container>
  );
};

export default AccessControl;
