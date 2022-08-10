import { Counter } from "./components/Counter";
import { FetchData } from "./components/FetchData";
import { ShowDay } from "./components/ShowDay";
import { Home } from "./components/Home";

const AppRoutes = [
  {
    index: true,
    element: <Home />
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
    path: '/show-day',
    element: <ShowDay />
  }
];

export default AppRoutes;
