import React, { createContext, useState, useEffect, useContext } from 'react';
import axios from 'axios';

// Create the context
const AuthContext = createContext();

// Custom hook to use the auth context
export const useAuth = () => {
  return useContext(AuthContext);
};

// Provider component
export const AuthProvider = ({ children }) => {
  const [authState, setAuthState] = useState({
    isAuthenticated: false,
    user: null,
    loading: true
  });

  // Initialize auth state from localStorage on component mount
  useEffect(() => {
    const initializeAuth = () => {
      try {
        const token = localStorage.getItem('token');
        const userStr = localStorage.getItem('user');

        if (token && userStr) {
          // Try to parse the user JSON
          let user;
          try {
            user = JSON.parse(userStr);
          } catch (e) {
            console.error('Failed to parse user data from localStorage:', e);
            throw new Error('Invalid user data');
          }

          // Validate user object
          if (!user || typeof user !== 'object') {
            throw new Error('Invalid user data format');
          }

          // Set the authorization header for all future requests
          axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;

          setAuthState({
            isAuthenticated: true,
            user: user,
            loading: false
          });
        } else {
          throw new Error('Missing authentication data');
        }
      } catch (error) {
        // Clear any potentially invalid auth data
        console.log('Auth initialization failed:', error.message);
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        delete axios.defaults.headers.common['Authorization'];

        setAuthState({
          isAuthenticated: false,
          user: null,
          loading: false
        });
      }
    };

    initializeAuth();
  }, []);

  // Login function
  const login = (authData) => {
    setAuthState({
      isAuthenticated: true,
      user: authData.user,
      loading: false
    });
  };

  // Logout function
  const logout = () => {
    // Clear auth data from localStorage
    localStorage.removeItem('token');
    localStorage.removeItem('user');

    // Remove authorization header
    delete axios.defaults.headers.common['Authorization'];

    // Update auth state
    setAuthState({
      isAuthenticated: false,
      user: null,
      loading: false
    });
  };

  // Update user function
  const updateUser = (userData) => {
    const updatedUser = { ...authState.user, ...userData };
    localStorage.setItem('user', JSON.stringify(updatedUser));

    setAuthState({
      ...authState,
      user: updatedUser
    });
  };

  // Context value
  const value = {
    ...authState,
    login,
    logout,
    updateUser
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};

export default AuthContext;
