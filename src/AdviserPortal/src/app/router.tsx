import { createBrowserRouter } from 'react-router-dom'
import { MainLayout } from '../layouts/MainLayout'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <MainLayout />,
    children: [
      {
        index: true,
        element: <div className="p-8 text-2xl">Dashboard placeholder</div>,
      },
    ],
  },
])