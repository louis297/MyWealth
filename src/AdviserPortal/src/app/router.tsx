import { createBrowserRouter } from 'react-router-dom'
import App from '../App'          // change to Layout later
// import LoginPage from '../features/auth/LoginPage'
// import DashboardPage from '../features/dashboard/DashboardPage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <App />,             // will change to Layout
    children: [
      {
        index: true,
        element: <div className="p-8 text-2xl">Dashboard placeholder</div>,
      },
      // {
      //   path: 'login',
      //   element: <LoginPage />,
      // },
      // {
      //   path: 'customers',
      //   element: <CustomersPage />,
      // },
    ],
  },
])