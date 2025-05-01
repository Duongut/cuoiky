import React, { useState } from 'react';
import { Card, ButtonGroup, Button } from 'react-bootstrap';
import { Line, Bar } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend,
  Filler,
} from 'chart.js';

// Register ChartJS components
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend,
  Filler
);

interface ActivityChartProps {
  title?: string;
  data: {
    labels: string[];
    datasets: {
      label: string;
      data: number[];
      borderColor: string;
      backgroundColor: string;
      tension: number;
    }[];
  };
  loading?: boolean;
  onDateRangeChange?: (days: number) => void;
  dateRange?: number;
}

const ActivityChart: React.FC<ActivityChartProps> = ({ 
  title, 
  data, 
  loading = false,
  onDateRangeChange,
  dateRange = 7
}) => {
  const [chartType, setChartType] = useState<'line' | 'bar'>('line');
  
  // Enhanced chart options
  const options = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: {
      mode: 'index' as const,
      intersect: false,
    },
    plugins: {
      legend: {
        display: false,
      },
      tooltip: {
        backgroundColor: 'rgba(255, 255, 255, 0.9)',
        titleColor: '#495057',
        bodyColor: '#495057',
        borderColor: '#e9ecef',
        borderWidth: 1,
        padding: 10,
        boxPadding: 6,
        usePointStyle: true,
        titleFont: {
          size: 13,
          weight: 'bold' as const,
        },
        bodyFont: {
          size: 13,
        },
        callbacks: {
          label: function(context: any) {
            let label = context.dataset.label || '';
            if (label) {
              label += ': ';
            }
            if (context.parsed.y !== null) {
              label += context.parsed.y + ' xe';
            }
            return label;
          }
        }
      },
    },
    scales: {
      y: {
        beginAtZero: true,
        grid: {
          drawBorder: false,
          color: 'rgba(0, 0, 0, 0.05)',
        },
        ticks: {
          font: {
            size: 11,
          },
          color: '#6c757d',
          padding: 8,
        }
      },
      x: {
        grid: {
          display: false,
        },
        ticks: {
          font: {
            size: 11,
          },
          color: '#6c757d',
          padding: 5,
        }
      },
    },
    elements: {
      point: {
        radius: 3,
        hoverRadius: 5,
        backgroundColor: 'white',
        borderWidth: 2,
      },
      line: {
        tension: 0.4,
        borderWidth: 2,
      },
      bar: {
        borderWidth: 0,
      },
    },
  };

  // Enhanced chart data with appropriate styling based on chart type
  const lineData = {
    labels: data.labels,
    datasets: data.datasets.map((dataset, index) => ({
      ...dataset,
      backgroundColor: index === 0 ? 'rgba(13, 110, 253, 0.1)' : 'rgba(220, 53, 69, 0.1)',
      fill: true,
    })),
  };

  const barData = {
    labels: data.labels,
    datasets: data.datasets.map((dataset, index) => ({
      ...dataset,
      backgroundColor: index === 0 ? 'rgba(13, 110, 253, 0.7)' : 'rgba(220, 53, 69, 0.7)',
      borderRadius: 3,
      maxBarThickness: 40,
    })),
  };

  return (
    <Card className="dashboard-card h-100">
      <Card.Header className="d-flex justify-content-between align-items-center">
        <div className="d-flex align-items-center">
          <h6 className="mb-0 me-3">{title || 'Thống kê xe vào/ra'}</h6>
          <div className="d-flex ms-2">
            <span className="d-flex align-items-center me-3 fs-8">
              <span className="status-indicator" style={{ backgroundColor: '#0d6efd' }}></span>
              Xe vào
            </span>
            <span className="d-flex align-items-center fs-8">
              <span className="status-indicator" style={{ backgroundColor: '#dc3545' }}></span>
              Xe ra
            </span>
          </div>
        </div>

        <div className="d-flex align-items-center">
          {/* Chart type toggle */}
          <ButtonGroup size="sm" className="me-3">
            <Button 
              variant={chartType === 'line' ? 'primary' : 'outline-primary'} 
              onClick={() => setChartType('line')}
              className="py-1 px-2"
            >
              <i className="bi bi-graph-up"></i>
            </Button>
            <Button 
              variant={chartType === 'bar' ? 'primary' : 'outline-primary'} 
              onClick={() => setChartType('bar')}
              className="py-1 px-2"
            >
              <i className="bi bi-bar-chart"></i>
            </Button>
          </ButtonGroup>
          
          {/* Date range selector */}
          {onDateRangeChange && (
            <ButtonGroup size="sm">
              <Button 
                variant={dateRange === 7 ? 'primary' : 'outline-primary'} 
                onClick={() => onDateRangeChange(7)}
                className="py-1 px-2"
              >
                7 ngày
              </Button>
              <Button 
                variant={dateRange === 14 ? 'primary' : 'outline-primary'} 
                onClick={() => onDateRangeChange(14)}
                className="py-1 px-2"
              >
                14 ngày
              </Button>
              <Button 
                variant={dateRange === 30 ? 'primary' : 'outline-primary'} 
                onClick={() => onDateRangeChange(30)}
                className="py-1 px-2"
              >
                30 ngày
              </Button>
            </ButtonGroup>
          )}
        </div>
      </Card.Header>
      <Card.Body>
        {loading ? (
          <div className="d-flex flex-column align-items-center justify-content-center h-100 py-5">
            <div className="spinner-border text-primary mb-3" role="status">
              <span className="visually-hidden">Đang tải...</span>
            </div>
            <p className="text-muted">Đang tải dữ liệu biểu đồ...</p>
          </div>
        ) : (
          <div className="chart-container" style={{ height: '300px' }}>
            {chartType === 'line' ? (
              <Line data={lineData} options={options} />
            ) : (
              <Bar data={barData} options={options} />
            )}
          </div>
        )}
      </Card.Body>
    </Card>
  );
};

export default ActivityChart;
