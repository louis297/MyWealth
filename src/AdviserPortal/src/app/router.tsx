import { createBrowserRouter } from 'react-router-dom'
import { AccountCreatePage } from '../features/accounts/AccountCreatePage'
import { AccountDetailPage } from '../features/accounts/AccountDetailPage'
import { AccountListPage } from '../features/accounts/AccountListPage'
import { AdvisersPage } from '../features/advisers/AdvisersPage'
import { LoginPage } from '../features/auth/LoginPage'
import { ProfilePage } from '../features/auth/ProfilePage'
import { CustomerCreatePage } from '../features/customers/CustomerCreatePage'
import { CustomerDetailPage } from '../features/customers/CustomerDetailPage'
import { CustomerListPage } from '../features/customers/CustomerListPage'
import { DashboardPage } from '../features/dashboard/DashboardPage'
import { TransactionsPage } from '../features/transactions/TransactionsPage'
import { AuthLayout } from '../layouts/AuthLayout'
import { MainLayout } from '../layouts/MainLayout'
import { RequireNavAccess } from '../layouts/RequireNavAccess'
import { NotFoundPage } from '../shared/components/NotFoundPage'

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <AuthLayout />,
    children: [{ index: true, element: <LoginPage />, handle: { title: 'Sign in' } }],
  },
  {
    path: '/',
    element: <MainLayout />,
    children: [
      {
        element: <RequireNavAccess />,
        children: [
          { index: true, element: <DashboardPage /> },
          { path: 'customers', element: <CustomerListPage /> },
          {
            path: 'customers/new',
            element: <CustomerCreatePage />,
            handle: { title: 'New customer' },
          },
          {
            path: 'customers/:customerId',
            element: <CustomerDetailPage />,
            handle: { title: 'Customer' },
          },
          { path: 'accounts', element: <AccountListPage /> },
          {
            path: 'accounts/new',
            element: <AccountCreatePage />,
            handle: { title: 'New account' },
          },
          {
            path: 'accounts/:accountId',
            element: <AccountDetailPage />,
            handle: { title: 'Account' },
          },
          { path: 'transactions', element: <TransactionsPage /> },
          { path: 'advisers', element: <AdvisersPage /> },
          { path: 'profile', element: <ProfilePage /> },
          { path: '*', element: <NotFoundPage />, handle: { title: 'Page not found' } },
        ],
      },
    ],
  },
])
