import React, { Component } from 'react';
import { Collapse, Navbar, NavbarBrand, NavbarToggler, NavItem, NavLink } from 'reactstrap';
import { Link } from 'react-router-dom';
import './NavMenu.css';

export class NavMenu extends Component {
  static displayName = NavMenu.name;

  constructor (props) {
    super(props);

    this.toggleNavbar = this.toggleNavbar.bind(this);
    this.state = {
      collapsed: true,
      siteinfo: {},
      loading: true
    };
  }

  componentDidMount() {
    this.populateSiteInfo();
  }

  toggleNavbar () {
    this.setState({
      collapsed: !this.state.collapsed
    });
  }

  render() {
    //let siteName = this.state.loading ? "lkwa.org": this.state.siteinfo.siteName;
    let siteName = "lkwa.org";
    return (
      <header>
        <Navbar className="navbar-expand-sm navbar-toggleable-sm ng-white border-bottom box-shadow mb-3" container light>
          <NavbarBrand tag={Link} to="/">{siteName}</NavbarBrand>
          <NavbarToggler onClick={this.toggleNavbar} className="mr-2" />
          <Collapse className="d-sm-inline-flex flex-sm-row-reverse" isOpen={!this.state.collapsed} navbar>
            <ul className="navbar-nav flex-grow">
              <NavItem>
                <NavLink tag={Link} className="text-dark" to="/">Home</NavLink>
              </NavItem>
              <NavItem>
                <NavLink tag={Link} className="text-dark" to="/show-day">日程 Schedules</NavLink>
              </NavItem>
              <NavItem>
                <NavLink tag={Link} className="text-dark" to="/ministry-report">传到 Ministry</NavLink>
              </NavItem>
              <NavItem>
                <NavLink tag={Link} className="text-dark" to="/login">Login</NavLink>
              </NavItem>
              <NavItem>

                </NavItem>
            </ul>
          </Collapse>
        </Navbar>
      </header>
    );
  }

  async populateSiteInfo() {
    const response = await fetch("api/siteinfo");
    if (!response.ok) {
      this.setState({
        siteinfo: {
          errorStatus: response.status,
          errorMessage: response.statusText,
          success: false
        },
        loading: false
      });
      return;
    }
    const data = await response.json();
    this.setState({ siteinfo: data, loading: false });
  }
}
