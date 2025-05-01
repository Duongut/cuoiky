import React from 'react';
import { Card, Table, Badge } from 'react-bootstrap';
import { FaCar, FaMotorcycle, FaClock, FaMapMarkerAlt } from 'react-icons/fa';

interface Vehicle {
  vehicleId: string;
  licensePlate: string;
  vehicleType: string;
  entryTime: string;
  exitTime?: string;
  slotId: string;
  status: string;
  isMonthlyRegistered?: boolean;
}

interface RecentVehiclesProps {
  vehicles: Vehicle[];
}

const RecentVehicles: React.FC<RecentVehiclesProps> = ({ vehicles }) => {
  // Format time considering Vietnamese locale
  const formatTime = (timeString: string) => {
    const date = new Date(timeString);
    return new Date(date).toLocaleString('vi-VN', {
      hour: '2-digit',
      minute: '2-digit',
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  };

  // Calculate duration for vehicles that have exited
  const calculateDuration = (entryTime: string, exitTime?: string) => {
    if (!exitTime) return '';
    
    const entry = new Date(entryTime);
    const exit = new Date(exitTime);
    const diff = exit.getTime() - entry.getTime();
    
    // Format the duration in hours and minutes
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    
    if (hours > 0) {
      return `${hours}h ${minutes}m`;
    } else {
      return `${minutes}m`;
    }
  };

  return (
    <Card className="dashboard-card">
      <Card.Header className="d-flex justify-content-between align-items-center">
        <h6 className="mb-0">Xe gần đây</h6>
        <Badge bg="primary" className="px-3 py-2 rounded-pill">
          {vehicles.length} xe
        </Badge>
      </Card.Header>
      <Card.Body className="p-0">
        <div className="table-responsive">
          <table className="table modern-table mb-0">
            <thead>
              <tr>
                <th>Biển số</th>
                <th>Thông tin</th>
                <th>Thời gian</th>
                <th>Vị trí</th>
                <th>Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              {vehicles.length === 0 ? (
                <tr>
                  <td colSpan={5} className="text-center py-4">
                    <div className="text-muted">Không có xe nào gần đây</div>
                  </td>
                </tr>
              ) : (
                vehicles.map(vehicle => (
                  <tr key={`${vehicle.vehicleId}-${vehicle.status}`}>
                    <td className="align-middle">
                      <div className="d-flex align-items-center">
                        <div className="me-2">
                          {vehicle.vehicleType === 'CAR' ? 
                            <FaCar className="text-primary" /> : 
                            <FaMotorcycle className="text-primary" />
                          }
                        </div>
                        <div>
                          <div className="fw-bold">{vehicle.licensePlate}</div>
                          <small className="text-muted">{vehicle.vehicleType === 'CAR' ? 'Ô tô' : 'Xe máy'}</small>
                        </div>
                      </div>
                    </td>
                    <td className="align-middle">
                      {vehicle.isMonthlyRegistered && (
                        <Badge bg="info-soft" className="me-1">Tháng</Badge>
                      )}
                      {vehicle.status === 'EXITED' && vehicle.exitTime && (
                        <Badge bg="warning-soft" className="me-1">{calculateDuration(vehicle.entryTime, vehicle.exitTime)}</Badge>
                      )}
                    </td>
                    <td className="align-middle">
                      <div className="d-flex align-items-center">
                        <FaClock className="text-muted me-1" size={12} />
                        <small>{formatTime(vehicle.entryTime)}</small>
                      </div>
                      {vehicle.status === 'EXITED' && vehicle.exitTime && (
                        <div className="d-flex align-items-center mt-1">
                          <span className="badge bg-secondary-soft me-1">Ra</span>
                          <small>{formatTime(vehicle.exitTime)}</small>
                        </div>
                      )}
                    </td>
                    <td className="align-middle">
                      <div className="d-flex align-items-center">
                        <FaMapMarkerAlt className="text-muted me-1" size={12} />
                        <span>{vehicle.slotId}</span>
                      </div>
                    </td>
                    <td className="align-middle">
                      <div className="d-flex align-items-center">
                        <span className={`status-indicator ${vehicle.status === 'PARKING' ? 'status-available' : 'status-occupied'}`}></span>
                        <Badge
                          bg={vehicle.status === 'PARKING' ? 'success-soft' : 'secondary-soft'}
                          className="text-nowrap"
                        >
                          {vehicle.status === 'PARKING' ? 'Đang đỗ' : 'Đã rời đi'}
                        </Badge>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </Card.Body>
    </Card>
  );
};

export default RecentVehicles;
