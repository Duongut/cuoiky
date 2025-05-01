import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Navbar as BootstrapNavbar, Nav, Container } from 'react-bootstrap';

const Navbar = () => {
  const location = useLocation();

  return (
    <BootstrapNavbar bg="dark" variant="dark" expand="lg">
      <Container>
        <BootstrapNavbar.Brand as={Link} to="/">Hệ Thống Bãi Đỗ Xe Thông Minh</BootstrapNavbar.Brand>
        <BootstrapNavbar.Toggle aria-controls="basic-navbar-nav" />
        <BootstrapNavbar.Collapse id="basic-navbar-nav">
          <Nav className="me-auto">
            <Nav.Link as={Link} to="/" active={location.pathname === '/'}>
              Trang chủ
            </Nav.Link>
            <Nav.Link
              as={Link}
              to="/access-control"
              active={location.pathname === '/access-control' ||
                     location.pathname === '/check-in' ||
                     location.pathname === '/check-out' ||
                     location.pathname === '/cameras'}
            >
              Kiểm soát ra vào
            </Nav.Link>
            <Nav.Link as={Link} to="/reports" active={location.pathname === '/reports'}>
              Báo cáo
            </Nav.Link>
            <Nav.Link as={Link} to="/monthly-registration" active={location.pathname === '/monthly-registration'}>
              Đăng ký xe tháng
            </Nav.Link>
          </Nav>
        </BootstrapNavbar.Collapse>
      </Container>
    </BootstrapNavbar>
  );
};

export default Navbar;
