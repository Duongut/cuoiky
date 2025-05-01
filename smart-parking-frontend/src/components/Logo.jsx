import React from 'react';

const Logo = ({ size = 24 }) => {
  return (
    <svg 
      width={size} 
      height={size} 
      viewBox="0 0 100 100" 
      xmlns="http://www.w3.org/2000/svg"
      style={{ minWidth: size }}
    >
      <circle cx="50" cy="50" r="45" fill="#f8f9fa" stroke="#1E88E5" strokeWidth="6" />
      <text
        x="50"
        y="65"
        fontFamily="Arial, sans-serif"
        fontSize="50"
        fontWeight="bold"
        fill="#1E88E5"
        textAnchor="middle"
      >
        P
      </text>
    </svg>
  );
};

export default Logo;