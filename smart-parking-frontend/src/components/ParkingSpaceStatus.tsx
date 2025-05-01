import React from 'react';
import { Card, ProgressBar } from 'react-bootstrap';
import { CircularProgressbar, buildStyles } from 'react-circular-progressbar';
import 'react-circular-progressbar/dist/styles.css';
import { FaSquare, FaCar, FaMotorcycle } from 'react-icons/fa';

interface ParkingSpaceStatusProps {
  title: string;
  occupancyRate: number;
  availableSpaces: number;
  occupiedSpaces: number;
  totalSpaces: number;
  carSpaces?: { total: number, available: number };
  motorcycleSpaces?: { total: number, available: number };
  loading?: boolean;
}

const ParkingSpaceStatus: React.FC<ParkingSpaceStatusProps> = ({
  title,
  occupancyRate,
  availableSpaces,
  occupiedSpaces,
  totalSpaces,
  carSpaces,
  motorcycleSpaces,
  loading = false,
}) => {
  // Calculate occupancy rates for car and motorcycle spaces
  const carOccupancyRate = carSpaces ? Math.round(((carSpaces.total - carSpaces.available) / carSpaces.total) * 100) : 0;
  const motorcycleOccupancyRate = motorcycleSpaces ? Math.round(((motorcycleSpaces.total - motorcycleSpaces.available) / motorcycleSpaces.total) * 100) : 0;

  // Determine color based on occupancy rate
  const getProgressColor = (rate: number) => {
    if (rate < 50) return '#28a745'; // Green for low occupancy
    if (rate < 80) return '#ffc107'; // Yellow for moderate occupancy
    return '#dc3545'; // Red for high occupancy
  };
  
  const progressColor = getProgressColor(occupancyRate);

  return (
    <Card className="dashboard-card h-100">
      <Card.Header className="d-flex justify-content-between align-items-center">
        <h6 className="mb-0">{title}</h6>
      </Card.Header>
      <Card.Body>
        {loading ? (
          <div className="d-flex flex-column align-items-center justify-content-center py-5">
            <div className="spinner-border text-primary mb-3" role="status">
              <span className="visually-hidden">Đang tải...</span>
            </div>
            <p className="text-muted">Đang tải dữ liệu...</p>
          </div>
        ) : (
          <>
            <div className="d-flex align-items-center justify-content-center mb-4">
              <div style={{ width: 140, height: 140 }}>
                <CircularProgressbar
                  value={occupancyRate}
                  text={`${occupancyRate}%`}
                  strokeWidth={10}
                  styles={buildStyles({
                    textSize: '16px',
                    pathColor: progressColor,
                    textColor: '#495057',
                    trailColor: '#e9ecef',
                  })}
                />
              </div>
            </div>

            <div className="row g-2 mb-4">
              <div className="col-6">
                <div className="bg-light p-3 rounded text-center h-100">
                  <div className="d-flex align-items-center justify-content-center mb-2">
                    <div className="status-indicator status-available me-2"></div>
                    <span className="fs-7 text-muted">Vị trí trống</span>
                  </div>
                  <h3 className="mb-0 fw-bold text-success">{availableSpaces}</h3>
                </div>
              </div>
              <div className="col-6">
                <div className="bg-light p-3 rounded text-center h-100">
                  <div className="d-flex align-items-center justify-content-center mb-2">
                    <div className="status-indicator status-occupied me-2"></div>
                    <span className="fs-7 text-muted">Vị trí đã sử dụng</span>
                  </div>
                  <h3 className="mb-0 fw-bold text-danger">{occupiedSpaces}</h3>
                </div>
              </div>
            </div>

            {(carSpaces || motorcycleSpaces) && (
              <div className="mt-4">
                <h6 className="mb-3 fs-7 text-muted">Chi tiết theo loại phương tiện</h6>
                
                {carSpaces && (
                  <div className="d-flex align-items-center mb-2">
                    <div className="me-2">
                      <FaCar className="text-primary" />
                    </div>
                    <div className="flex-grow-1">
                      <div className="d-flex justify-content-between mb-1">
                        <span className="fs-8">Ô tô: {carSpaces.available}/{carSpaces.total}</span>
                        <span className="fs-8 fw-bold">{carOccupancyRate}%</span>
                      </div>
                      <ProgressBar 
                        now={carOccupancyRate} 
                        variant={getProgressColor(carOccupancyRate) === '#28a745' ? 'success' : 
                                 getProgressColor(carOccupancyRate) === '#ffc107' ? 'warning' : 'danger'} 
                        style={{ height: '6px' }}
                      />
                    </div>
                  </div>
                )}
                
                {motorcycleSpaces && (
                  <div className="d-flex align-items-center">
                    <div className="me-2">
                      <FaMotorcycle className="text-primary" />
                    </div>
                    <div className="flex-grow-1">
                      <div className="d-flex justify-content-between mb-1">
                        <span className="fs-8">Xe máy: {motorcycleSpaces.available}/{motorcycleSpaces.total}</span>
                        <span className="fs-8 fw-bold">{motorcycleOccupancyRate}%</span>
                      </div>
                      <ProgressBar 
                        now={motorcycleOccupancyRate} 
                        variant={getProgressColor(motorcycleOccupancyRate) === '#28a745' ? 'success' : 
                                 getProgressColor(motorcycleOccupancyRate) === '#ffc107' ? 'warning' : 'danger'}
                        style={{ height: '6px' }}
                      />
                    </div>
                  </div>
                )}
              </div>
            )}
          </>
        )}
      </Card.Body>
    </Card>
  );
};

export default ParkingSpaceStatus;
