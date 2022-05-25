# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),

## 2022-05-24
### Fixed
- Added HasMultipleMeetingsPerWeek for schedules that have multiple meetings per week, 
  even every day, that need to start on the same day every week, instead of always 
  starting on today.

### Changed
- Add AssignmentApi project in a test attempt to turn this into a few small microservices.
- Use ScheduleInput as a parameter for ScheduleLoader.GetSchedule() instead of many 
  string parameters.

## 2022-04-26
### Changed
- Set timeout for SMTP send to 90 seconds.
- Pause 10 seconds after each send.
- Mark each recipient status as sent, don't let it try again.  Rely on logs.
- Add meetingName parameter to /schedules/generate/{meetingName}/{key}

## 2022-04-25
### Fixed
- This fix is not in the source code the fix is in the HTML template files I'm using.  the HTML header should include:
  `<meta charset="utf-8" />` so that accented characters like ñ don't show extra symbols.

## 2022-04-24
### Fixed
- Fixed week calculation where current week, starting Monday, is calculated by the current date.
