import { Counter } from "./components/Counter";
import { FetchData } from "./components/FetchData";
import { ShowDay } from "./components/ShowDay";
import { Home } from "./components/Home";

const AppRoutes = [
  {
    index: true,
        element: <ShowDay />
  },
  {
    path: '/counter',
    element: <Counter />
  },
  {
    path: '/fetch-data',
    element: <FetchData />
  },
  {
    path: '/day/:date/:key',
    element: <ShowDay />
  }
];

export default AppRoutes;
