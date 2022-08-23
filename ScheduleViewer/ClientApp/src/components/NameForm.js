import React, { Component } from 'react';

export class NameForm extends Component {
  static displayName = NameForm.name;
  constructor(props) {
    super(props);
    this.state = {
      value: '',
      month: 9,
      hours: '0',
      placements: '0',
      videos: 0
    };
    //const [inputs, setInputs] = useState({});

    this.handleChange = this.handleChange.bind(this);
    this.handleSubmit = this.handleSubmit.bind(this);
  }

  handleChange(event) {
    this.setState({ value: event.target.value });
  }

  handleSubmit(event) {
    alert('A name was submitted: ' + this.state.value);
    event.preventDefault();
  }

  render() {
    return (
      <div class="container-sm">
      <h3>传到报告 Service Report</h3>
        <div class="row">
          <form onSubmit={this.handleSubmit}>
            <div class="col-sm-4 mb-3 mt-3">
              <label for="email" class="form-label mb-0">姓名 Name:</label>
              <input type="text" class="form-control" id="email" name="email" placeholder="Enter your name" value={this.state.value} onChange={this.handleChange} />
            </div>
            <div class="col-sm-4 mb-3 mt-3">
              <label for="email" class="form-label mb-0">姓名 Month:</label>
              <select value={this.state.month} class="form-select" onChange={this.handleChange}>
                <option value="1">一月 January</option>
                <option value="2">二月 February</option>
                <option value="3">三月 March</option>
                <option value="4">四月 April</option>
                <option value="5">五月 May</option>
                <option value="6">六月 June</option>
                <option value="7">七月 July</option>
                <option value="8">八月 August</option>
                <option value="9">九月 September</option>
                <option value="10">十月 October</option>
                <option value="11">十一月 November</option>
                <option value="12">十二月 December</option>
              </select>              
            </div>
            <div class="col-sm-4 mb-3 mt-3">
              <label for="placements" class="form-label mb-0">放置文献 Placements:</label>
              <input type="text" class="form-control" id="placements" name="placements" placeholder="Enter placements (printed and electronic)" value={this.state.placements} onChange={this.handleChange} />
            </div>
            <div class="col-sm-4 mb-3 mt-3">
              <label for="videos" class="form-label mb-0">显示了多少视频？ Video Showings:</label>
              <input type="text" class="form-control" id="videos" name="videos" placeholder="Enter number of video showings" value={this.state.vues} onChange={this.handleChange} />
            </div>
            <div class="input-group">
              <div class="col-sm-4 mb-3 mt-3">
                <label for="hours" class="form-label mb-0">小时 Hours:</label>
                <input type="text" class="form-control" id="hours" name="hours" placeholder="Enter number of hours" value={this.state.value} onChange={this.handleChange} />
                <label for="less-than-hour" class="form-check-label">不到一个小时 &lt; 1 hour:</label>
                <input class="form-check-input  mx-3" type="checkbox" id="less-than-hour" name="option1" value="something" checked />
              </div>
            </div>
            <div class="col-sm-4 mb-3 mt-3">
              <label for="return-visits" class="form-label mb-0">回访 Return Visits:</label>
              <input type="text" class="form-control" id="return-visits" name="return-visits" placeholder="Enter return visits" value={this.state.value} onChange={this.handleChange} />
            </div>
            <div class="col-sm-4 mb-3 mt-3">
              <label for="bible-studies" class="form-label mb-0">圣经学生 Bible Studies:</label>
              <input type="number" step="1" min="0" class="form-control" id="bible-studies" name="bible-studies" placeholder="Number of different bible studies conducted" value={this.state.value} onChange={this.handleChange} />
            </div>
            <input type="submit" class="btn btn-primary" value="Submit" />
              
          </form>
        </div>
      </div>
    );
  }
}