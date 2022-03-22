import time

class stopwatch:
	def __init__(self):
		self.start_time = 0
		self.end_time = 0

	def elapsed(self):
		return self.time_convert(self.end_time - self.start_time)

	def time_convert(self, sec):
		mins = sec // 60
		sec = sec % 60

		hours = mins // 60
		mins = mins % 60
		return "{0:0>2d}:{1:0>2d}:{2:0>9f}".format(int(hours), int(mins), sec)

	def start(self):
		self.start_time = time.time()
		self.end_time = self.start_time

	def stop(self):
		self.end_time = time.time()
	
	def restart(self):
		self.start_time = 0
		self.end_time = 0
