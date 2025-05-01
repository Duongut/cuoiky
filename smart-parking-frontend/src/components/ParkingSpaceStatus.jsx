import React from 'react';
import { Card, ProgressBar } from 'react-bootstrap';
import { FaCarAlt, FaMotorcycle } from 'react-icons/fa';
import Skeleton from 'react-loading-skeleton';
import 'react-loading-skeleton/dist/skeleton.css';

const ParkingSpaceStatus = ({
  title,
  occupancyRate,
  availableSpaces,
  occupiedSpaces,
  reservedSpaces,
  totalSpaces,
  carSpaces,
  motorcycleSpaces,
  loading
}) => {
  // Function to determine progress bar variant based on occupancy rate
  const getProgressBarVariant = (rate) => {
    if (rate < 50) return 'success';
    if (rate < 80) return 'warning';
    return 'danger';
  };

  // Calculate percentages for the stacked progress bar
  const occupiedPercent = Math.round((occupiedSpaces / totalSpaces) * 100) || 0;
  const reservedPercent = Math.round((reservedSpaces / totalSpaces) * 100) || 0;
  const availablePercent = 100 - occupiedPercent - reservedPercent;

  return (
    <Card className="shadow-sm h-100">
      <Card.Header className="bg-white border-0 pt-3">
        <h5 className="card-title mb-0">{title}</h5>
      </Card.Header>
      <Card.Body>
        {loading ? (
          <>
            <Skeleton height={40} />
            <div className="mt-4">
              <Skeleton count={4} height={30} className="mb-2" />
            </div>
          </>
        ) : (
          <>
            {/* Stacked progress bar showing occupied, reserved and available spaces */}
            <div className="mb-3">
              <div className="d-flex justify-content-between mb-1">
                <span>Tỷ lệ sử dụng</span>
                <span className="fw-bold">{occupancyRate}%</span>
              </div>
              <div className="progress" style={{ height: '20px' }}>
                <div 
                  className="progress-bar bg-danger" 
                  style={{ width: `${occupiedPercent}%` }}
                >
                  {occupiedPercent > 10 && `${occupiedSpaces} đang sử dụng`}
                </div>
                <div 
                  className="progress-bar bg-warning" 
                  style={{ width: `${reservedPercent}%` }}
                >
                  {reservedPercent > 10 && `${reservedSpaces} đặt trước`}
                </div>
                <div 
                  className="progress-bar bg-success" 
                  style={{ width: `${availablePercent}%` }}
                >
                  {availablePercent > 10 && `${availableSpaces} còn trống`}
                </div>
              </div>
              <div className="d-flex mt-1 text-muted small">
                <div className="me-3">
                  <span className="badge bg-danger">&nbsp;</span> Đang sử dụng
                </div>
                <div className="me-3">
                  <span className="badge bg-warning">&nbsp;</span> Đặt trước
                </div>
                <div>
                  <span className="badge bg-success">&nbsp;</span> Còn trống
                </div>
              </div>
            </div>

            {/* Detailed status */}
            <div className="mt-4">
              <table className="table table-sm">
                <tbody>
                  <tr>
                    <td><FaCarAlt className="text-primary me-2" /> Ô tô</td>
                    <td>
                      <div className="d-flex justify-content-between">
                        <span>Còn trống:</span>
                        <span className="fw-bold">{carSpaces.available}/{carSpaces.total}</span>
                      </div>
                    </td>
                  </tr>
                  <tr>
                    <td><FaMotorcycle className="text-primary me-2" /> Xe máy</td>
                    <td>
                      <div className="d-flex justify-content-between">
                        <span>Còn trống:</span>
                        <span className="fw-bold">{motorcycleSpaces.available}/{motorcycleSpaces.total}</span>
                      </div>
                    </td>
                  </tr>
                  <tr className="table-light">
                    <td>Tổng chỗ trống</td>
                    <td className="fw-bold text-end">{availableSpaces}/{totalSpaces}</td>
                  </tr>
                  <tr className="table-warning">
                    <td>Chỗ đặt trước (xe tháng)</td>
                    <td className="fw-bold text-end">{reservedSpaces}/{totalSpaces}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </>
        )}
      </Card.Body>
    </Card>
  );
};

export default ParkingSpaceStatus;