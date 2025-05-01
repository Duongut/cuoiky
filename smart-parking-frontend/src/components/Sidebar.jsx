import React, { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { Nav, Button, Dropdown } from 'react-bootstrap';
import Logo from './Logo';
import { FaChartBar, FaExchangeAlt, FaFileAlt, FaCarAlt, FaChevronRight, FaCog, FaSignOutAlt, FaUser, FaKey, FaUserCircle } from 'react-icons/fa';
import { useAuth } from '../contexts/AuthContext';
import ChangePasswordModal from './ChangePasswordModal';

const Sidebar = ({ onToggle, userRole }) => {
  const location = useLocation();
  const navigate = useNavigate();
  const { logout, user } = useAuth();
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [showChangePasswordModal, setShowChangePasswordModal] = useState(false);

  const toggleSidebar = () => {
    setIsCollapsed(!isCollapsed);
    if (onToggle) {
      onToggle(!isCollapsed);
    }
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const openChangePasswordModal = () => {
    setShowChangePasswordModal(true);
  };

  return (
    <div className={`sidebar ${isCollapsed ? 'collapsed' : ''}`}>
      <div className="sidebar-header">
        <div className="brand d-flex align-items-center">
          <Logo size={28} />
          {!isCollapsed && (
            <>
              <div className="ms-2 brand-text">
                <div className="brand-title">Smart</div>
                <div className="brand-title">Parking</div>
              </div>
            </>
          )}
          <button className={`collapse-btn ${isCollapsed ? 'collapsed-btn' : ''}`} onClick={toggleSidebar}>
            <FaChevronRight />
          </button>
        </div>
      </div>

      <div className="sidebar-menu mt-4">
        <Nav className="flex-column">
          {/* User info section */}
          <Dropdown className={`user-dropdown ${isCollapsed ? 'collapsed' : ''} mb-3`}>
            <Dropdown.Toggle variant="link" id="user-dropdown" className="user-dropdown-toggle">
              <div className="d-flex align-items-center px-3 py-2">
                <div className="user-icon">
                  <FaUserCircle />
                </div>
                {!isCollapsed && (
                  <div className="ms-2 user-details">
                    <div className="user-name">{user?.firstName} {user?.lastName}</div>
                    <div className="user-role">{user?.role === 'ADMIN' ? 'Administrator' : 'Staff'}</div>
                    <div className="user-id small text-muted">ID: {user?.employeeId}</div>
                  </div>
                )}
              </div>
            </Dropdown.Toggle>

            <Dropdown.Menu>
              <Dropdown.Item onClick={openChangePasswordModal}>
                <FaKey className="me-2" /> Change Password
              </Dropdown.Item>
              <Dropdown.Divider />
              <Dropdown.Item onClick={handleLogout}>
                <FaSignOutAlt className="me-2" /> Logout
              </Dropdown.Item>
            </Dropdown.Menu>
          </Dropdown>

          {/* Dashboard - available to all users */}
          <Nav.Link
            as={Link}
            to="/"
            className={location.pathname === '/' ? 'active' : ''}
          >
            <div className="d-flex align-items-center">
              <div className="menu-icon">
                <FaChartBar />
              </div>
              {!isCollapsed && <span className="ms-3">Dashboard</span>}
            </div>
          </Nav.Link>

          {/* Access Control - available to all users */}
          <Nav.Link
            as={Link}
            to="/access-control"
            className={location.pathname.startsWith('/access-control') || ['/check-in', '/check-out', '/cameras'].includes(location.pathname) ? 'active' : ''}
          >
            <div className="d-flex align-items-center">
              <div className="menu-icon">
                <FaExchangeAlt />
              </div>
              {!isCollapsed && <span className="ms-3">Access Control</span>}
            </div>
          </Nav.Link>

          {/* Reports - available to all users */}
          <Nav.Link
            as={Link}
            to="/reports"
            className={location.pathname === '/reports' ? 'active' : ''}
          >
            <div className="d-flex align-items-center">
              <div className="menu-icon">
                <FaFileAlt />
              </div>
              {!isCollapsed && <span className="ms-3">Reports</span>}
            </div>
          </Nav.Link>

          {/* Monthly Registration - available to all users */}
          <Nav.Link
            as={Link}
            to="/monthly-registration"
            className={location.pathname === '/monthly-registration' ? 'active' : ''}
          >
            <div className="d-flex align-items-center">
              <div className="menu-icon">
                <FaCarAlt />
              </div>
              {!isCollapsed && <span className="ms-3">Đăng ký xe tháng</span>}
            </div>
          </Nav.Link>

          {/* Settings - only available to admin users */}
          {userRole === 'ADMIN' && (
            <Nav.Link
              as={Link}
              to="/settings"
              className={location.pathname === '/settings' ? 'active' : ''}
            >
              <div className="d-flex align-items-center">
                <div className="menu-icon">
                  <FaCog />
                </div>
                {!isCollapsed && <span className="ms-3">Settings</span>}
              </div>
            </Nav.Link>
          )}


        </Nav>
      </div>

      {/* Change Password Modal */}
      <ChangePasswordModal
        show={showChangePasswordModal}
        onHide={() => setShowChangePasswordModal(false)}
      />
    </div>
  );
};

export default Sidebar;