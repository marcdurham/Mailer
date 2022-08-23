import ApiAuthorzationRoutes from './components/api-authorization/ApiAuthorizationRoutes';
import { Counter } from "./components/Counter";
import { FetchData } from "./components/FetchData";
import { ShowDay } from "./components/ShowDay";
import { NameForm } from "./components/NameForm";
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
    path: '/day/:date/:key',
    element: <ShowDay />
  },
  {
    path: '/ministry-report',
    element: <NameForm />
  },
  {
    path: '/fetch-data',
    requireAuth: true,
    element: <FetchData />
  },
  ...ApiAuthorzationRoutes

];

export default AppRoutes;
