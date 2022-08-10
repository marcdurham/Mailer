import React, { Component } from 'react';

export class ShowDay extends Component {
    static displayName = ShowDay.name;

  constructor(props) {
    super(props);
      this.state = { emaildata: [], loading: true };
  }

    componentDidMount() {
        const queryParams = new URLSearchParams(window.location.search);
        const date = queryParams.get("date");
      this.populateDay(date);
  }

    static renderDayTable(emaildata) {
        return (
          <table className='table table-striped' aria-labelledby="tabelLabel">
            <thead>
              <tr>
                <th>Name</th>
                <th>Value</th>
              </tr>
            </thead>
            <tbody>
              {emaildata.rows.map(row =>
                <tr key={row.name}>
                  <td>{row.name}</td>
                  <td>{row.value}</td>
                </tr>
              )}
            </tbody>
          </table>
    );
  }

    render() {
        const queryParams = new URLSearchParams(window.location.search);
        const date = queryParams.get("date");
            
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
        : ShowDay.renderDayTable(this.state.emaildata);

    return (
      <div>
        <h1 id="tabelLabel" >Show Day</h1>
            <p>This should show the calendar.</p>
            <h3> {date}</h3>
        {contents}
      </div>
    );
  }

  async populateDay(date) {
      const response = await fetch(`emaildata?date=${date}`);
    const data = await response.json();
    this.setState({ emaildata: data, loading: false });
  }
}
