import React from 'react';
import { Card } from 'react-bootstrap';
import { Line } from 'react-chartjs-2';
import { FaArrowUp, FaArrowDown } from 'react-icons/fa';

interface StatCardProps {
  title: string;
  value: string | number;
  trend?: {
    value: number;
    isPositive?: boolean;
  };
  icon?: string;
  iconBg?: string;
  trendData?: number[];
  color?: string;
  loading?: boolean;
}

const StatCard: React.FC<StatCardProps> = ({ 
  title, 
  value, 
  trend, 
  icon, 
  iconBg, 
  trendData = [4, 2, 5, 3, 5, 6, 7],
  color = '#0d6efd',
  loading = false 
}) => {
  const chartData = {
    labels: ['', '', '', '', '', '', ''],
    datasets: [
      {
        data: trendData,
        borderColor: color,
        backgroundColor: 'transparent',
        pointBackgroundColor: 'transparent',
        pointBorderColor: 'transparent',
        borderWidth: 2,
        tension: 0.4,
      },
    ],
  };

  const chartOptions = {
    responsive: true,
    plugins: {
      legend: {
        display: false,
      },
      tooltip: {
        enabled: false,
      },
    },
    scales: {
      x: {
        display: false,
      },
      y: {
        display: false,
      },
    },
    elements: {
      point: {
        radius: 0,
      },
    },
    maintainAspectRatio: false,
  };

  return (
    <Card className="stat-card h-100 border-0 shadow-sm">
      <Card.Body className="position-relative">
        {icon && (
          <div className={`stat-icon-container ${iconBg || ''} position-absolute top-0 end-0 mt-2 me-2`}>
            <img src={icon} alt={title} className="stat-icon" />
          </div>
        )}
        
        <div className="text-muted mb-1 fs-7">{title}</div>
        
        <div className="d-flex align-items-center">
          {loading ? (
            <div className="stat-value placeholder-glow">
              <span className="placeholder col-6"></span>
            </div>
          ) : (
            <div className="stat-value fw-bold" style={{ fontSize: '1.5rem' }}>{value}</div>
          )}
        </div>
        
        {trend && (
          <div className="d-flex align-items-center mt-1">
            <span className={`badge bg-${trend.isPositive ? 'success' : 'danger'}-soft me-1 d-flex align-items-center`}>
              {trend.isPositive ? <FaArrowUp size={10} className="me-1" /> : <FaArrowDown size={10} className="me-1" />}
              {trend.value}%
            </span>
            <span className="text-muted small">so với hôm qua</span>
          </div>
        )}
      </Card.Body>
      
      {trendData && (
        <div className="chart-wrapper px-2" style={{ height: '40px' }}>
          <Line data={chartData} options={chartOptions} />
        </div>
      )}
    </Card>
  );
};

export default StatCard;
