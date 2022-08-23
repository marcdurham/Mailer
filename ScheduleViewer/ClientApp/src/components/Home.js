import React, { Component } from 'react';

export class Home extends Component {
  static displayName = Home.name;

  render() {
    return (
      <div>
        <h1>信息板 Information Board</h1>
        <p>A centralized place to find stuff...</p>
        <div class="alert alert-warning">
          <strong>Attention!</strong> Meeting in-person this week!
        </div>
        <h5>聚会 Meetings</h5>
        <a href="https://schedules.lkwa.org/?date=2022-08-13&key=B3CF86C4-3F6C-4CDD-9D03-0F9323C95B39">
          <button class="btn btn-primary m-2">日程 Schedules</button>
          </a>
        <button class="btn btn-primary m-2">Zoom</button>
        <h5>传到 Ministry</h5>
        <button class="btn btn-primary m-2">传到日程 Schedules</button>
        <a href="https://drive.google.com/drive/folders/1HlAWAXRuIiOSecRleejfoHxQkwNMVXM_?usp=sharing">
          <button class="btn btn-primary m-2" >Google Drive Folder</button>
        </a>
        <a href="/ministry-report">
          <button class="btn btn-primary m-2" >传到报告 Ministry Report</button>
          </a>
        <button class="btn btn-primary m-2">Zoom</button>
        
        <a href="https://iron.territorytools.org">
          <button class="btn btn-primary m-2">区域 Territory</button>
        </a>
        <h5>会众 Congregation</h5>
        <button class="btn btn-primary m-2">Group Assignments</button>
        <a href="https://docs.google.com/spreadsheets/d/1AdNs1-F0EQYMTxFfWj9J9elv9sTt6qLiT8cNo_CAHbw/edit?usp=sharing">
          <button class="btn btn-primary m-2">EMS Emergency</button>
        </a>

        <p>Please contact so-and-so for help with:</p>
        <ul>
          <li><strong>Changes</strong>. Email <a href="somebody@gmail.com">somebody@gmail.com</a> for help with the new campaign.</li>
          <li><strong>Territory</strong>. Talk to <a href="">Caleb Chang</a> or <a href="">Marc Durham</a> about territories</li>
          
        </ul>
        <p>This is a secure site</p>
      </div>
    );
  }
}

